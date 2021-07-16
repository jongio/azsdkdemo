using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using DotNetEnv;
using Lib.Tunnel;

namespace Lib.Configuration
{
    public static class Config
    {
        private static HttpClient http = new HttpClient();
        private static ITunnelClient tunnelClient;

        static Config()
        {
            Envs.Load();
        }

        public static ITunnelClient TunnelClient
        {
            get
            {
                if (tunnelClient == null)
                {
                    tunnelClient = TunnelClientFactory.Get(Config.TunnelType, http);
                    tunnelClient.Initialize();
                }
                return tunnelClient;
            }
        }

        public static string BaseName { get { return Env.GetString("BASENAME"); } }
        public static bool UseAzurite { get { return Env.GetBool("USE_AZURITE", false); } }

        public static bool UseAzuriteBlob { get { return Env.GetBool("USE_AZURITE_BLOB", UseAzurite); } }
        public static bool UseAzuriteQueue { get { return Env.GetBool("USE_AZURITE_QUEUE", UseAzurite); } }
        public static bool UseAzuriteTable { get { return Env.GetBool("USE_AZURITE_TABLE", UseAzurite); } }

        public static string AzuriteAccountName { get { return Env.GetString("AZURITE_ACCOUNT_NAME", "devstoreaccount1"); } }
        public static string AzuriteAccountKey { get { return Env.GetString("AZURITE_ACCOUNT_KEY", "Eby8vdM02xNOcqFlqUwJPLlmEtlCDXJ1OUzFT50uSRZ6IFsuFq2UVErCz4I6tq/K1SZFPTOtr/KBHBeksoGMGw=="); } }
        public static string AzuriteConnectionString { get { return Env.GetString("AZURITE_CONNECTION_STRING", $"DefaultEndpointsProtocol=http;AccountName={AzuriteAccountName};AccountKey={AzuriteAccountKey};BlobEndpoint={StorageBlobEndpoint.ToString()};QueueEndpoint={StorageQueueEndpoint.ToString()};TableEndpoint={StorageTableEndpoint.ToString()};"); } }
        public static string AzuriteProxyConnectionString { get { return Env.GetString("AZURITE_PROXY_CONNECTION_STRING", $"DefaultEndpointsProtocol=http;AccountName={AzuriteAccountName};AccountKey={AzuriteAccountKey};BlobEndpoint={StorageBlobProxyEndpoint.ToString()};QueueEndpoint={StorageQueueProxyEndpoint.ToString()};TableEndpoint={StorageTableEndpoint.ToString()};"); } }
        public static string TunnelType { get { return Env.GetString("TUNNEL_TYPE", "NGROK"); } }

        public static bool UseCosmosEmulator { get { return Env.GetBool("USE_COSMOS_EMULATOR", false); } }

        public static string CosmosEndpoint
        {
            get
            {
                return Env.GetString("AZURE_COSMOS_ENDPOINT", UseCosmosEmulator ? "https://localhost:8081" : $"https://{BaseName}cosmosaccount.documents.azure.com:443");
            }
        }

        public static string CosmosKey
        {
            get
            {
                return Env.GetString("AZURE_COSMOS_KEY", UseCosmosEmulator ? "C2y6yDjf5/R+ob0N8A7Cgv30VRDJIWEHLM+4QDU5DE2nQ9nDuVTqobD4b8mGGyPMbIZnqyMsEcaGQy67XIw/Jw==" : "");
            }
        }

        public static int CosmosThroughput { get { return Env.GetInt("AZURE_COSMOS_THROUGHPUT", 400); } }
        public static string CosmosDB { get { return Env.GetString("AZURE_COSMOS_DB", "memealyzer"); } }
        public static string CosmosCollection { get { return Env.GetString("AZURE_COSMOS_COLLECTION", "images"); } }
        public static string CosmosKeySecretName { get { return Env.GetString("AZURE_COSMOS_KEY_SECRET_NAME", "CosmosKey"); } }
        public static Uri FormRecognizerEndpoint { get { return new Uri(Env.GetString("AZURE_FORM_RECOGNIZER_ENDPOINT", $"https://{BaseName}fr.cognitiveservices.azure.com/")); } }
        public static Uri KeyVaultEndpoint { get { return new Uri(Env.GetString("AZURE_KEYVAULT_ENDPOINT", $"https://{BaseName}kv.vault.azure.net/")); } }

        public static string StorageAccountName { get { return Env.GetString("AZURE_STORAGE_ACCOUNT_NAME", UseAzurite ? AzuriteAccountName : $"{BaseName}storage"); } }
        public static string StorageBlobAccountName { get { return Env.GetString("AZURE_STORAGE_BLOB_ACCOUNT_NAME", UseAzuriteBlob ? AzuriteAccountName : $"{BaseName}storage"); } }
        public static string StorageQueueAccountName { get { return Env.GetString("AZURE_STORAGE_QUEUE_ACCOUNT_NAME", UseAzuriteQueue ? AzuriteAccountName : $"{BaseName}storage"); } }
        public static string StorageTableAccountName { get { return Env.GetString("AZURE_STORAGE_TABLE_ACCOUNT_NAME", UseAzuriteTable ? AzuriteAccountName : $"{BaseName}storage"); } }
        public static TimeSpan StorageQueueMaxPollingInterval { get { return TimeSpan.Parse(Env.GetString("AZURE_STORAGE_QUEUE_MAX_POLLING_INTERVAL", "00:00:10")); } }
        public static Uri StorageBlobEndpoint { get { return GetStorageEndpoint("blob", 10000, UseAzuriteBlob, false); } }
        public static Uri StorageBlobProxyEndpoint { get { return GetStorageEndpoint("blob", 10000, UseAzuriteBlob, true); } }
        public static Uri StorageQueueEndpoint { get { return GetStorageEndpoint("queue", 10001, UseAzuriteQueue, false); } }
        public static Uri StorageQueueProxyEndpoint { get { return GetStorageEndpoint("queue", 10001, UseAzuriteQueue, true); } }
        public static Uri StorageTableEndpoint { get { return GetStorageEndpoint("table", 10002, UseAzuriteTable, false); } }

        public static Uri GetStorageEndpoint(string type, int port, bool emulator, bool proxy)
        {
            var endpoint = Env.GetString($"AZURE_STORAGE_{type.ToUpper()}_ENDPOINT", $"https://{BaseName}storage.{type.ToLower()}.core.windows.net/");
            if (emulator)
            {
                if (proxy && TunnelClient.Tunnels.ContainsKey(port))
                {
                    endpoint = TunnelClient.Tunnels[port].ToString() + AzuriteAccountName;
                }
                else
                {
                    endpoint = $"http://127.0.0.1:{port}/{AzuriteAccountName}";
                }
            }

            return new Uri(endpoint);
        }

        public static Uri TextAnalyticsEndpoint { get { return new Uri(Env.GetString("AZURE_TEXT_ANALYTICS_ENDPOINT", $"https://{BaseName}ta.cognitiveservices.azure.com/")); } }
        public static Uri AppConfigEndpoint { get { return new Uri(Env.GetString("AZURE_APP_CONFIG_ENDPOINT", $"https://{BaseName}appconfig.azconfig.io")); } }
        public static string ServiceBusNamespace { get { return Env.GetString("AZURE_SERVICE_BUS_NAMESPACE", $"{BaseName}sb.servicebus.windows.net"); } }
        public static string StorageBlobContainerName { get { return Env.GetString("AZURE_STORAGE_BLOB_CONTAINER_NAME", "blobs"); } }
        public static string MessagesQueueName { get { return Env.GetString("AZURE_MESSAGES_QUEUE_NAME", "messages"); } }
        public static int StorageQueueMessageCount { get { return Env.GetInt("AZURE_STORAGE_QUEUE_MSG_COUNT", 10); } }
        public static int StorageQueueReceiveSleep { get { return Env.GetInt("AZURE_STORAGE_QUEUE_RECEIVE_SLEEP", 1); } }
        public static string StorageTableName { get { return Env.GetString("AZURE_STORAGE_TABLE_NAME", "images"); } }
        public static string StorageType { get { return Env.GetString("AZURE_STORAGE_TYPE", "COSMOS_SQL"); } }
        public static string MessagingType { get { return Env.GetString("AZURE_MESSAGING_TYPE", "SERVICE_BUS_QUEUE"); } }
        public static string ClientSyncQueueName { get { return Env.GetString("AZURE_CLIENT_SYNC_QUEUE_NAME", "sync"); } }
        public static string SignalRConnectionStringSecretName { get { return Env.GetString("AZURE_SIGNALR_CONNECTION_STRING_SECRET_NAME", "SignalRConnectionString"); } }
        public static string ImageEndpoint { get { return Env.GetString("IMAGE_ENDPOINT", "https://meme-api.herokuapp.com/gimme/wholesomememes"); } }
        public static bool IsDevelopment { get { return string.IsNullOrEmpty(Env.GetString("WEBSITE_INSTANCE_ID")); } }
    }
}