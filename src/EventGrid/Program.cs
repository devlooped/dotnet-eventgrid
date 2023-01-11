using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using DotNetConfig;
using Microsoft.AspNetCore.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Serilog;

namespace Devlooped;

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
        var config = Config.Build();
        var url = config.GetString("eventgrid", "url");

        var argList = new List<string>();
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

        if (config.GetString("eventgrid", "jq") is string jq && jq.Length > 0)
            argList.Add("jq=" + jq);

        if ((args.Length == 0 && url == null) ||
            args[0] == "-?" ||
            args[0] == "-h" ||
            args[0] == "--help")
        {
            Console.WriteLine("Usage: eventgrid [url] -[property]* +[property[=minimatch]]* [jq=expression]");
            Console.WriteLine("      +all                    Render all properties");
            Console.WriteLine("      -property               Exclude a property");
            Console.WriteLine("      +property[=minimatch]   Include a property, optionally filtering ");
            Console.WriteLine("                              with the given the minimatch expression.");
            Console.WriteLine("      jq=expression           When rendering event data containing JSON, ");
            Console.WriteLine("                              apply the given JQ expression. Learn more at ");
            Console.WriteLine("                              https://stedolan.github.io/jq/");
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("- Include all event properties, for topic ending in 'System'");
            Console.WriteLine("      eventgrid https://mygrid.com +all +topic=**/System");
            Console.WriteLine();
            Console.WriteLine("- Exclude data property and filter for specific event types");
            Console.WriteLine("      eventgrid https://mygrid.com -data +eventType=Login");
            Console.WriteLine();
            Console.WriteLine("- Filter using synthetized path property '{domain}/{topic}/{subject}/{eventType}'");
            Console.WriteLine("      eventgrid https://mygrid.com +path=MyApp/**/Login");
            Console.WriteLine();
            Console.WriteLine("- Filter using synthetized path property for a specific event and user (subject)");
            Console.WriteLine("      eventgrid https://mygrid.com +path=MyApp/*/1bQUI/Login");
            Console.WriteLine();
            Console.WriteLine("Note: all matches are case insensitive");
            return 0;
        }

        argList.AddRange(args);

        var filter = Filter.Parse(argList);
        var renderer = Renderer.Parse(argList);

        Console.WriteLine(filter.ToString());
        Console.WriteLine(renderer.ToString());
        Console.WriteLine();

        // CLI-provided URL should override config provided one
        if (Uri.TryCreate(args[0], UriKind.Absolute, out var uri))
            url = args[0];

        if (url == null)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Valid Url must be provided via command line arguments or .netconfig");
            return -1;
        }

        var connection = new HubConnectionBuilder()
            .WithUrl(url)
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

        connection.On<string>("event", async e =>
        {
            try
            {
                var evt = JsonConvert.DeserializeObject<PathEventGridEvent>(e, settings);
                if (evt != null && filter.ShouldInclude(evt))
                    logger.Information("{event}", await renderer.RenderAsync(evt));
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
