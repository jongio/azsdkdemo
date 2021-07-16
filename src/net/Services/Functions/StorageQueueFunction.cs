using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace Memealyzer.Functions
{
    public static class StorageQueueFunction
    {
        [FunctionName(nameof(StorageQueueFunctionRun))]
        public static async Task StorageQueueFunctionRun(
            [QueueTrigger("%ClientSyncQueueName%", Connection = "StorageConnection")] string message,
            [SignalR(HubName = "STORAGE_QUEUE")] IAsyncCollector<SignalRMessage> messages,
            ILogger log)
        {
            log.LogInformation(message);
            await Messages.SendAsync(messages, message);
        }
    }
}