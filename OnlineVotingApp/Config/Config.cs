using Serilog;
using Shared;
using System.IO;

namespace OnlineVotingApp.Config
{
    class Config : IConfig
    {
        public static readonly string KEY_HOSTED_ONLINE = "HostedOnline"; 
        public static readonly string KEY_HOST_ONLINE = "HostOnline";
        public static readonly string KEY_PORT_ONLINE = "PortOnline";

        public bool HostedOnline { get; set; }
        public string HostOnline { get; set; }
        public string PortOnline { get; set; }
        public string DirectoryOnline { get; set; }
        public string QueryOnline { get; set; }

        private ILogger logger = Log.Logger.ForContext<Config>();
        private OptionsFile optionsFile;
        
        public Config(string file)
        {
            if (!File.Exists(file))
            {
                logger.Warning($"online config file \"{file}\" not found");
            } else
            {
                // If the file does exist, read its content
                optionsFile = new OptionsFile(file);
                optionsFile.ReadFile();
                
                HostedOnline = optionsFile.ReadValueBool("HostedOnline", false);
                HostOnline = optionsFile.ReadValue("HostOnline");
                PortOnline = optionsFile.ReadValue("PortOnline");
                DirectoryOnline = optionsFile.ReadValue("DirectoryOnline");
                QueryOnline = optionsFile.ReadValue("QueryOnline");
            }
        }
    }
}
