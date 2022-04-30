using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVotingApp.Config
{
    interface IConfig
    {
        public bool HostedOnline { get; set; }
        public string HostOnline { get; set; }
        public string PortOnline { get; set; }
        public string DirectoryOnline { get; set; }
        public string QueryOnline { get; set; }
    }
}
