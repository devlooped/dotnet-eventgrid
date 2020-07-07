using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Serilog;

namespace Devlooped
{
    class Program
    {
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

            connection.Closed += arg => { logger.Error(arg, "Closed: {Error}"); return Task.CompletedTask; };
            connection.Reconnected += arg => { logger.Information("Reconnected: {Message}", arg); return Task.CompletedTask; };
            connection.Reconnecting += arg => { logger.Error(arg, "Reconnecting: {Error}"); return Task.CompletedTask; };

            connection.On<string>("event", e => logger.Information("{Message}", e));

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
