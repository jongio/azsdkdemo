using System;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Azure.Core.Diagnostics;
using Lib;
using Lib.Configuration;
using Lib.Tunnel;

namespace QueueService
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //using var listener = AzureEventSourceListener.CreateConsoleLogger();

            await using var clients = new Clients();
            await clients.InitializeAsync();

            while (true)
            {
                Console.WriteLine("Receiving Messages...");

                // Get Messages
                var messages = await clients.MessagingProvider.ImageQueueClient.ReceiveMessagesAsync();

                foreach (var message in messages)
                {
                    try
                    {
                        Console.WriteLine(message.Message.Text);

                        // Extract Text from Image
                        var blobUri = message.Image.GetBlobUri(Config.UseAzuriteBlob, Config.StorageBlobProxyEndpoint.ToString(), Config.StorageBlobContainerName);
                        try
                        {
                            var recognizeContentOperation = await clients.FormRecognizerClient.StartRecognizeContentFromUriAsync(new Uri(blobUri));
                            var recognizeContentCompletion = await recognizeContentOperation.WaitForCompletionAsync();
                            var content = recognizeContentCompletion.Value;
                            var text = content.SelectMany(page => page.Lines).Aggregate(new StringBuilder(), (a, b) =>
                            {
                                a.Append($"{b.Text} ");
                                return a;
                            });

                            message.Image.Text = text.ToString();
                        }
                        catch (Exception ex)
                        {
                            message.Image.Error = ex.Message.ToString();
                            Console.WriteLine(ex.ToString());
                        }


                        if (!string.IsNullOrEmpty(message.Image.Text))
                        {
                            try
                            {
                                Console.WriteLine($"Image Text: {message.Image.Text}");

                                // Analyize Text Sentiment
                                var documentSentiment = await clients.TextAnalyticsClient.AnalyzeSentimentAsync(message.Image.Text);
                                message.Image.Sentiment = documentSentiment.Value.Sentiment.ToString();

                                Console.WriteLine($"Image Sentiment: {message.Image.Sentiment}");
                            }
                            catch (Exception ex)
                            {
                                message.Image.Error = ex.Message.ToString();
                                Console.WriteLine(ex.ToString());
                            }
                        }
                        else
                        {
                            message.Image.Text = "No Text Extracted from Image.";
                            message.Image.Sentiment = "None";
                            Console.WriteLine(message.Image.Text);
                        }

                        message.Image.Status = "Completed";

                        // Save Document
                        message.Image = await clients.DataProvider.UpsertImageAsync(message.Image);

                        Console.WriteLine($"Document Saved: {message.Image.Id}");

                        // Delete Queue Message
                        var deleteResponse = await clients.MessagingProvider.ImageQueueClient.DeleteMessageAsync(message);

                        Console.WriteLine($"Queue Message Deleted: {message.Message.Id}");

                        // Enqueue message to Client Sync Queue
                        await clients.MessagingProvider.ClientSyncQueueClient.SendMessageAsync(message, true);
                        Console.WriteLine($"Added to Client Sync Queue: {message.Message.Id}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                await Task.Delay(TimeSpan.FromSeconds(Config.StorageQueueReceiveSleep));
            }
        }
    }
}