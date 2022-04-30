using Newtonsoft.Json;
using OnlineVotingApp.ChaosPipe;
using Serilog;
using System;
using System.Threading;
using OnlineVotingApp.WebServerHosting;


namespace OnlineVotingApp
{
    class OnlineVotingApp
    {

        private static ILogger logger;
        public static void Main(string[] args)
        {
            if (args.Length < 1 || Array.IndexOf(args, "--startOnline") > -1)
            {
                Console.WriteLine("Please don't start the voting server process manually as it's only supposed to be launched by the mod itself."
                    + "\nPass --startOnline as an argument if you want to start the server yourself for debugging purposes.");

                Console.ReadKey();
                return;
            }

            Log.Logger = new LoggerConfiguration()
               .MinimumLevel.Debug()
               .WriteTo.File("./chaosmod/onlineApp.log", outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] [{SourceContext:l}] {Message:lj}{NewLine}{Exception}")
               .CreateLogger();
            logger = Log.Logger.ForContext<OnlineVotingApp>();

            logger.Information("=============================");
            logger.Information("Starting chaos mod online app");
            logger.Information("=============================");

            var config = new Config.Config("./chaosmod/online.ini");

            Mutex mutex = new Mutex(false, "ChaosModVVotingMutex");
            mutex.WaitOne();

            try
            {
                // Start the pipe
                var chaosPipe = new ChaosPipeClient();

                // Start the chaos mod controller
                new ChaosModController(chaosPipe, config);

                while (chaosPipe.IsConnected()) { }
            }
            finally
            {
                mutex.ReleaseMutex();
            }
            logger.Information("Pipe disconnected, ending program");
            Environment.Exit(0);
        }
    }
}
