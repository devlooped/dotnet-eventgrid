using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DotNetConfig;
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
                Console.WriteLine("Usage: eventgrid [url] -[property]* +[property[=minimatch]]*");
                Console.WriteLine("      +all                    Render all properties");
                Console.WriteLine("      -property               Exclude a property");
                Console.WriteLine("      +property[=minimatch]   Include a property, optionally filtering ");
                Console.WriteLine("                              with the given the minimatch expression.");
                Console.WriteLine();
                Console.WriteLine("Examples:");
                Console.WriteLine("- Include all event properties, for topic ending in 'System'");
                Console.WriteLine("      eventstream https://mygrid.com +all +topic=**/System");
                Console.WriteLine();
                Console.WriteLine("- Exclude data property and filter for specific event types");
                Console.WriteLine("      eventstream https://mygrid.com -data +eventType=Login");
                Console.WriteLine();
                Console.WriteLine("- Filter using synthetized path property '{domain}/{topic}/{subject}/{eventType}'");
                Console.WriteLine("      eventstream https://mygrid.com +path=MyApp/**/Login");
                Console.WriteLine();
                Console.WriteLine("- Filter using synthetized path property for a specific event and user (subject)");
                Console.WriteLine("      eventstream https://mygrid.com +path=MyApp/*/1bQUI/Login");
                Console.WriteLine();
                Console.WriteLine("Note: all matches are case insensitive");
                return 0;
            }

            var config = Config.Build();
            var argList = new List<string>(args);

            foreach (var include in config.GetAll("eventgrid", "filter")
                .Concat(config.GetAll("eventgrid", "include"))
                .Where(x => !string.IsNullOrEmpty(x.RawValue)))
            {
                argList.Add("+" + include.RawValue!.TrimStart('+'));
            }
            foreach (var exclude in config.GetAll("eventgrid", "exclude").Where(x => !string.IsNullOrEmpty(x.RawValue)))
            {
                argList.Add("-" + exclude.RawValue!.TrimStart('-'));
            }

            var filter = Filter.Parse(argList);
            var renderer = Renderer.Parse(argList);

            Console.WriteLine(filter.ToString());
            Console.WriteLine(renderer.ToString());
            Console.WriteLine();

            var connection = new HubConnectionBuilder()
                .WithUrl(args[0])
                .WithAutomaticReconnect()
                .Build();

            var logger = new LoggerConfiguration()
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            connection.Closed += arg => { logger.Error(arg, "Closed"); return Task.CompletedTask; };
            connection.Reconnected += arg => { logger.Information("Reconnected"); return Task.CompletedTask; };
            connection.Reconnecting += arg => { logger.Error("Reconnecting: {Message}", args.FirstOrDefault()); return Task.CompletedTask; };

            connection.On<string>("event", e =>
            {
                try
                {
                    var evt = JsonConvert.DeserializeObject<PathEventGridEvent>(e, settings);
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
