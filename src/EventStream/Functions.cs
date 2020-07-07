using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.EventGrid.Models;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.EventGrid;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.SignalRService;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Devlooped
{
    public class Functions
    {
        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            Converters =
            {
                new IsoDateTimeConverter
                {
                    DateTimeFormat = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK",
                    DateTimeStyles = DateTimeStyles.AdjustToUniversal
                },
            },
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            DateFormatString = "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffK",
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };

        [FunctionName("negotiate")]
        public IActionResult Negotiate(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] HttpRequest req,
            [SignalRConnectionInfo(HubName = "events")] SignalRConnectionInfo connectionInfo)
        {
            var expectedKey = Environment.GetEnvironmentVariable("AccessKey");
            if (string.IsNullOrEmpty(expectedKey))
                return new OkObjectResult(connectionInfo);

            var accessKey = req.Query["accessKey"];
            if (StringValues.IsNullOrEmpty(accessKey) ||
                !StringValues.Equals(expectedKey, accessKey))
                return new UnauthorizedResult();

            return new OkObjectResult(connectionInfo);
        }

        [FunctionName("event")]
        public Task PostEventAsync(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post")] string message,
            [SignalR(HubName = "events")] IAsyncCollector<SignalRMessage> messages)
            => messages.AddAsync(new SignalRMessage
            {
                Target = "event",
                Arguments = new[] { JsonConvert.SerializeObject(new
                {
                    subject = "Unknown",
                    topic = "Unknown",
                    eventType = "Unknown",
                    eventTime = DateTime.UtcNow,
                    data = message,
                    dataVersion = typeof(Functions).Assembly.GetName().Version?.ToString(3)
                }, settings)}
            });

        [FunctionName("publish")]
        public Task EventAsync(
            [EventGridTrigger] EventGridEvent e,
            [SignalR(HubName = "events")] IAsyncCollector<SignalRMessage> messages)
        {
            // Simplify topic which is otherwise unwieldy with useless info
            var parts = new List<string>(e.Topic.Split('/'));
            var domains = parts.IndexOf("domains");
            var topics = parts.IndexOf("topics");

            if (domains != -1 && topics != -1)
            {
                var values = parts.ToArray();
                e.Topic = string.Join('/', values[(domains + 1)..topics]) + "/" + string.Join('/', values[(topics + 1)..]);
            }

            return messages.AddAsync(new SignalRMessage
            {
                Target = "event",
                Arguments = new[] { JsonConvert.SerializeObject(e, settings) }
            });
        }
    }
}
