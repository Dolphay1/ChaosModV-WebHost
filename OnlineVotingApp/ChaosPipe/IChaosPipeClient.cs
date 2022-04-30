﻿
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVotingApp.ChaosPipe
{
    interface IChaosPipeClient
    {
        /// <summary>
        /// Event that gets invoked when the chaos mod requests voting results
        /// </summary>
        event EventHandler<OnGetVoteResultArgs> OnGetVoteResult;
        /// <summary>
        /// Event that gets invoked when the chaos mod creates a new vote
        /// </summary>
        event EventHandler<OnNewVoteArgs> OnNewVote;
        /// <summary>
        /// Event that gets invoked when the chaos mod starts a no voting round
        /// </summary>
        event EventHandler OnNoVotingRound;

        public bool IsConnected();
    }
}
