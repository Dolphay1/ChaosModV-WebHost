using Serilog;
using System.Timers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineVotingApp.Config;
using OnlineVotingApp.ChaosPipe;
using OnlineVotingApp.WebServerHosting;

namespace OnlineVotingApp
{
    class ChaosModController
    {
        public static readonly int DISPLAY_UPDATE_TICKRATE = 200;

        private List<IVoteOption> activeVoteOptions = new List<IVoteOption>();
        private IChaosPipeClient chaosPipe;
        private System.Timers.Timer displayUpdateTick = new System.Timers.Timer(DISPLAY_UPDATE_TICKRATE);
        private ILogger logger = Log.Logger.ForContext<ChaosModController>();
        private Dictionary<string, int> userVotedFor = new Dictionary<string, int>();
        private bool voteRunning = false;
        private bool hosted = false;
        private bool ssl = false; //to be implemented
        private string host;
        private string query;
        private WebServer webServer;

        public ChaosModController (
            IChaosPipeClient chaosPipe,
            IConfig config
        )
        {
            this.chaosPipe = chaosPipe;

            // Setup pipe listeners
            this.chaosPipe.OnGetVoteResult += OnGetVoteResult;
            this.chaosPipe.OnNewVote += OnNewVote;
            this.chaosPipe.OnNoVotingRound += OnNoVotingRound;

            hosted = config.HostedOnline;

            if(hosted)
            {
                //Get the host and add the port, as well as the directory
                host = config.HostOnline + ":" + config.PortOnline + (config.DirectoryOnline.StartsWith("/") ? config.DirectoryOnline : "/"+config.DirectoryOnline);
                //To be implemented: SSL Mode
                host = ssl ? "https" : "http" + "" + host;
            }
            else
            {
                webServer = new WebServer(config, chaosPipe);
            }
        }

        /// <summary>
        /// Send a web request or <> to get the results of the votes.
        /// </summary>

        private void OnGetVoteResult(object sender, OnGetVoteResultArgs e)
        {
            if (hosted)
            {
                WebRequests.OnGetVoteResult(e, host + "?" + query + "=");
            }
            else
            {
                webServer.OnGetResaults(e);
            }
        }

        /// <summary>
        /// 
        /// </summary>

        private void OnNewVote(object sender, OnNewVoteArgs e)
        {

            voteRunning = true;

            if(hosted)
            {
                WebRequests.OnNewVote(e, host);
            }
            else
            {
                webServer.OnNewVote(e);
            }
        }

        private void OnNoVotingRound(object sender, EventArgs e)
        {
            if(hosted)
            {
                WebRequests.OnNoVotingRound(host);
            }
            else
            {
                webServer.OnNonVotingRound();
            }
        }
    }
}
