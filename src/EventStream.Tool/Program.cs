using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Serialization;
using Serilog;

namespace Devlooped
{
    class Program
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

        static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Usage: eventstream [url]");
                return -1;
            }

            var connection = new HubConnectionBuilder()
                .WithUrl(args[0])
                .WithAutomaticReconnect()
                .Build();

            var config = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}{Exception}");

            var logger = config.CreateLogger();

            connection.Closed += arg => { logger.Error(arg, "Closed"); return Task.CompletedTask; };
            connection.Reconnected += arg => { logger.Information("Reconnected"); return Task.CompletedTask; };
            connection.Reconnecting += arg => { logger.Error(arg, "Reconnecting"); return Task.CompletedTask; };

            connection.On<string>("event", e =>
            {
                try
                {
                    var evt = JsonConvert.DeserializeObject<EventGridEvent>(e, settings);
                    try
                    {
                        // Try unpacking the data so it renders better.
                        evt.Data = JsonConvert.DeserializeObject((string)evt.Data, settings);
                    }
                    catch { }

                    logger.Information("{Message}", JsonConvert.SerializeObject(new
                    {
                        id = evt.Id,
                        eventTime = evt.EventTime,
                        eventType = evt.EventType,
                        subject = evt.Subject,
                        topic = evt.Topic,
                        data = evt.Data
                    }, settings));
                }
                catch 
                {
                    logger.Information("{Message}", e);
                }
            });

            try
            {
                await connection.StartAsync();
                logger.Information("Connected");
                Console.ReadLine();
                return 0;
            }
            catch (Exception e)
            {
                logger.Error(e, "Error: {Error}");
                return 0;
            }

        }
    }
}
