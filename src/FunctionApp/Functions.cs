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
        static readonly JsonSerializerSettings settings = new JsonSerializerSettings
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
            if (StringValues.IsNullOrEmpty(accessKey))
                accessKey = req.Query["key"];
            if (StringValues.IsNullOrEmpty(accessKey))
                accessKey = req.Query["k"];

            if (StringValues.IsNullOrEmpty(accessKey) ||
                !StringValues.Equals(expectedKey, accessKey))
                return new UnauthorizedResult();

            return new OkObjectResult(connectionInfo);
        }

        [FunctionName("publish")]
        public Task EventAsync(
            [EventGridTrigger] EventGridEvent e,
            [SignalR(HubName = "events")] IAsyncCollector<SignalRMessage> messages)
        {
            return messages.AddAsync(new SignalRMessage
            {
                Target = "event",
                Arguments = new[] { JsonConvert.SerializeObject(e, settings) }
            });
        }
    }
}
