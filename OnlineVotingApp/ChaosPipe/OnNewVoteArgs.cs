using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineVotingApp.ChaosPipe
{
    class OnNewVoteArgs
    {
        public string[] VoteOptionNames { get; }

        public OnNewVoteArgs(string[] voteOptionNames)
        {
            VoteOptionNames = voteOptionNames;
        }
    }
}
