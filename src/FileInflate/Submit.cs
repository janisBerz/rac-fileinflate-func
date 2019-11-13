using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;

namespace AzUnzipEverything
{
    public static class Submit
    {
        [FunctionName("Submit")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [OrchestrationClient] DurableOrchestrationClient submit,
            ILogger log)
        {
            var sourceGetStatusKey = Environment.GetEnvironmentVariable("GetStatusKey");

            //log.LogInformation("C# HTTP trigger function processed a request.");

            string name = req.Query["name"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            name = name ?? data?.name;

            if (name == null)
            {
                ActionResult badresponse = new BadRequestResult();
                return badresponse;
            }

            log.LogInformation($"About start orchestrator for {name}");

            var instanceId = await submit.StartNewAsync("Orchestrator", name);
            log.LogInformation($"Submission process started {instanceId}");

            string checkStatusLocacion = string.Format("{0}://{1}/api/status/{2}?code={3}", req.Scheme, req.Host, instanceId, sourceGetStatusKey); // To inform the client where to check the status
            string message = ($"Your submission has been received. To get the status, go to: {checkStatusLocacion}");

            // Create an Http Response with Status Accepted (202) to let the client know that the request has been accepted but not yet processed. 
            ActionResult response = new AcceptedResult(checkStatusLocacion, message); // The GET status location is returned as an http header
            req.HttpContext.Response.Headers.Add("x-functions-key", sourceGetStatusKey); // add getstatus key as header 
            req.HttpContext.Response.Headers.Add("retry-after", "3"); // To inform the client how long to wait in seconds before checking the status

            return response;
        }
    }
}
