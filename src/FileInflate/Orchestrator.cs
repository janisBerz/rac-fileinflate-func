using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace AzUnzipEverything
{
    public static class Orchestrator
    {
        [FunctionName("Orchestrator")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] DurableOrchestrationContext context)
        {

            var outputs = new List<string>();
            var blobName = context.GetInput<string>();
            //var unZipThis = await context.CallActivityAsync<string>("unZip", blobName);

            //// Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>("unZip", blobName));
            //outputs.Add(await context.CallActivityAsync<string>("Orchestrator_Hello", "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>("Orchestrator_Hello", "London"));

            //// returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }
    }
}