using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Logging;

namespace Memealyzer.Functions
{
    public static class NegotiateFunction
    {
        [FunctionName(nameof(Negotiate))]
        public static SignalRConnectionInfo Negotiate(
                    [HttpTrigger(AuthorizationLevel.Anonymous)] HttpRequest req,
                    [SignalRConnectionInfo(HubName = "%MessagingType%")] SignalRConnectionInfo connectionInfo, ILogger log)
        {
            log.LogInformation(connectionInfo.ToString());   
            return connectionInfo;
        }
    }
}