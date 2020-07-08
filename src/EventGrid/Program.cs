using System;
using System.Globalization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Azure.EventGrid.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
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
            if (args.Length == 0 || args[0] == "-?" || 
                args[0] == "-h" || args[0] == "--help")
            {
                Console.WriteLine("Usage: eventstream [url] -[property]* +[property:minimatch]*");
                Console.WriteLine("      -property             Don't show property in rendered output.");
                Console.WriteLine("      +property:minimatch   Filter entries where the specified property ");
                Console.WriteLine("                            matches the given minimatch expression.");
                return 0;
            }

            var filter = Filter.Parse(args);
            var renderer = Renderer.Parse(args);

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
                    if (filter.ShouldInclude(evt))
                        logger.Information("{event}", renderer.Render(evt));
                }
                catch 
                {
                    logger.Information("{event}", e);
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
