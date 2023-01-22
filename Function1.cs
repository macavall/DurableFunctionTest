using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.DurableTask;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace DurableFunctionTest
{
    public static class Function1
    {
        public static int counter = 0;
        public static int orchTimerDelaySec = Convert.ToInt32(Environment.GetEnvironmentVariable("orchTimerDelaySec"));
        public static int forLoopCount = Convert.ToInt32(Environment.GetEnvironmentVariable("forLoopCount"));
        public static int threadSleepMs = Convert.ToInt32(Environment.GetEnvironmentVariable("threadSleepMs"));
        public static string url = Environment.GetEnvironmentVariable("url");

        [FunctionName("Function1")]
        public static async Task<List<string>> RunOrchestrator(
            [OrchestrationTrigger] IDurableOrchestrationContext context, ILogger log)
        {
            var myTask = Task.Factory.StartNew(async () =>
            {
            if (counter < 10)
            {
                // for loop ten times
                for (int i = 0; i < forLoopCount; i++)
                    {
                        //await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo");
                        //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));

                        await SendHttpRequest(log);
                    }
                    
                    //Thread.Sleep(10000);
                }
            });

            counter++;

            DateTime dueTime = context.CurrentUtcDateTime.AddSeconds(orchTimerDelaySec);
            await context.CreateTimer(dueTime, CancellationToken.None);

            // Get this thread id
            int threadId = Thread.CurrentThread.ManagedThreadId;


            //await myTask;

            // Put the orchestrator to sleep for 72 hours

            var outputs = new List<string>();

            // Replace "hello" with the name of your Durable Activity Function.
            outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Tokyo"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "Seattle"));
            //outputs.Add(await context.CallActivityAsync<string>(nameof(SayHello), "London"));

            // returns ["Hello Tokyo!", "Hello Seattle!", "Hello London!"]
            return outputs;
        }

        [FunctionName(nameof(SayHello))]
        public static async Task<string> SayHello([ActivityTrigger] string name, ILogger log)
        {
            //await SendHttpRequest(log);

            Thread.Sleep(threadSleepMs);

            log.LogInformation($"Saying hello to {name}.");
            return $"Hello {name}!";
        }

        [FunctionName("Function1_HttpStart")]
        public static async Task<HttpResponseMessage> HttpStart(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestMessage req,
            [DurableClient] IDurableOrchestrationClient starter,
            ILogger log)
        {
            // Function input comes from the request content.
            string instanceId = await starter.StartNewAsync("Function1", null);

            log.LogInformation($"Started orchestration with ID = '{instanceId}'.");

            return starter.CreateCheckStatusResponse(req, instanceId);
        }

        public static async Task SendHttpRequest(ILogger log)
        {
            // create http client
            var client = new HttpClient();

            // create request message
            var request = new HttpRequestMessage(HttpMethod.Get, "https://" + url + "/api/Function1_HttpStart");

            // send request
            var response = await client.SendAsync(request);

            log.LogInformation("Response: " + response.StatusCode);
        }
    }
}