using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace AzUnzipEverything
{
    public static class GetStatus
    {
        /// <summary>
        /// Http Triggered Function which acts as a wrapper to get the status of a running Durable orchestration instance.
        /// It enriches the response based on the GetStatusAsync's retruned value
        /// </summary>
        [FunctionName("GetStatus")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, methods: "get", Route = "status/{instanceId}")] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient orchestrationClient,
            string instanceId,
            ILogger logger)
        {
            var sourceGetStatusKey = Environment.GetEnvironmentVariable("GetStatusKey");

            // Get the built-in status of the orchestration instance. This status is managed by the Durable Functions Extension. 
            var status = await orchestrationClient.GetStatusAsync(instanceId);
            if (status != null)
            {
   
                if (status.RuntimeStatus == OrchestrationRuntimeStatus.Running || status.RuntimeStatus == OrchestrationRuntimeStatus.Pending)
                {
                    //The URL (location header) is prepared so the client know where to get the status later. 
                    string checkStatusLocacion = string.Format("{0}://{1}/api/status/{2}?code={3}", req.Scheme, req.Host, instanceId, sourceGetStatusKey);
                    string message = $"The zip file is being processed. The current status is ''. To check the status later, go to: GET {checkStatusLocacion}"; // To inform the client where to check the status

                    // Create an Http Response with Status Accepted (202) to let the client know that the original request hasn't yet been fully processed. 
                    ActionResult response = new AcceptedResult(checkStatusLocacion, message); // The GET status location is returned as an http header
                    //req.HttpContext.Response.Headers.Add("x-functions-key", sourceGetStatusKey); // add getstatus key as header 
                    req.HttpContext.Response.Headers.Add("retry-after", "10"); // To inform the client how long to wait before checking the status. 
                    return response;
                }
                else if (status.RuntimeStatus == OrchestrationRuntimeStatus.Completed)
                {
                    // Once the orchestration has been completed, an Http Response with Status OK (200) is created to inform the client that the original request has been fully processed. 
                    
                        return new OkObjectResult($"Congratulations, your presentation with id '{instanceId}' has been Completed!");
                }
            }
            // If status is null, then instance has not been found. Create and return an Http Response with status NotFound (404). 
            return new NotFoundObjectResult($"Whoops! Something went wrong. Please check if your submission Id is correct. Submission '{instanceId}' not found.");
        }
    }
}