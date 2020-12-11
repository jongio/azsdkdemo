using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Azure.AI.FormRecognizer;
using Azure.AI.TextAnalytics;
using Azure.Data.AppConfiguration;
using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Lib.Data;
using Lib.Images;
using Lib.Messaging;
using Lib.Model;

namespace Lib
{
    public class Clients : IAsyncDisposable
    {
        public SecretClient SecretClient;
        public ChainedTokenCredential credential = Identity.GetCredentialChain();
        public BlobServiceClient BlobServiceClient;
        public BlobContainerClient ContainerClient;
        public IMessagingProvider MessagingProvider;
        public TextAnalyticsClient TextAnalyticsClient;
        public IImageProvider ImageProvider;
        public string BingImageSearchTerm;
        public string BingImageSearchApiKey;
        public FormRecognizerClient FormRecognizerClient;
        public ConfigurationClient ConfigurationClient;
        public IDataProvider DataProvider;
        private HttpClient httpClient = new HttpClient();

        public static Clients Instance { get; private set; }

        public Clients()
        {
            Instance = this;
        }

        public async Task InitializeAsync()
        {
            // Data Provider            
            DataProvider = DataProviderFactory.Get(Config.StorageType);
            await DataProvider.InitializeAsync(credential);

            // Messaging Provider            
            MessagingProvider = MessagingProviderFactory.Get(Config.MessagingType);
            await MessagingProvider.InitializeAsync(credential, DataProvider);

            // Blob
            BlobServiceClient = new BlobServiceClient(Config.StorageBlobEndpoint, credential);
            ContainerClient = BlobServiceClient.GetBlobContainerClient(Config.StorageBlobContainerName);
            await ContainerClient.CreateIfNotExistsAsync(PublicAccessType.BlobContainer);

            await RefreshAppConfiguration();

            // FormRecognizerClient
            FormRecognizerClient = new FormRecognizerClient(Config.FormRecognizerEndpoint, credential);

            // TextAnalyticsClient
            TextAnalyticsClient = new TextAnalyticsClient(Config.TextAnalyticsEndpoint, credential);
        }

        public Task RefreshAppConfiguration()
        {
            // App Config
            ConfigurationClient = new ConfigurationClient(Config.AppConfigEndpoint, credential);
            
            // Image Provider
            ImageProvider = ImageProviderFactory.Get(
                ConfigurationClient.GetConfigurationSetting("imageProvider").Value.Value
            );

            // Bing image search settings
            BingImageSearchTerm = ConfigurationClient.GetConfigurationSetting("BingImageSearchTerm").Value.Value;
            BingImageSearchApiKey = ConfigurationClient.GetConfigurationSetting("BingImageSearchApiKey").Value.Value;

            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            DataProvider.Dispose();
            return MessagingProvider.DisposeAsync();
        }

        public async Task<Image> EnqueueImageAsync(Image image = null)
        {
            if (image?.Url is null || string.IsNullOrEmpty(image.Url))
            {
                // Get the Image from the provider
                image = await ImageProvider.GetImage();
            }

            // Get Image Stream
            using var imageStream = await httpClient.GetStreamAsync(image.Url);

            // Upload to Blob
            var blobClient = ContainerClient.GetBlobClient(image.BlobName);
            await blobClient.UploadAsync(imageStream);

            Console.WriteLine($"Uploaded to Blob Storage: {blobClient.Uri}");

            image.BlobUri = blobClient.Uri.ToString();

            // Send Queue Message
            var sendReceipt = await MessagingProvider.ImageQueueClient.SendMessageAsync(new ImageQueueMessage { Image = image });

            //Console.WriteLine($"Added to Queue: {sendReceipt.Message.Id}");
            return image;
        }
    }
}
