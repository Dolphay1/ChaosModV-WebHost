using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using OnlineVotingApp.ChaosPipe;
using OnlineVotingApp.Config;


namespace OnlineVotingApp
{
    static class WebRequests
    {

        private static readonly string CONTENT_TYPE_JSON = "application/json";
        private static readonly string CONTENT_TYPE_TEXT = "text/plain";

        private static readonly string VOTE_RESULT_REQUEST = "result";

        /// <summary>
        /// Send a post request to the specified host.
        /// </summary>
        private static async void PostRequest(string request, string mediatype, string host)
        {
            using (var client = new HttpClient())
            {
                var response = await client.PostAsync(host, new StringContent(request, Encoding.UTF8, mediatype));
            }
        }

        /// <summary>
        /// Send a get request to the specified host.
        /// </summary>
        private static string GetRequest(string host)
        {
            using (var client = new HttpClient())
            {
                var response = client.GetAsync(host);
                response.Wait();
                var content = response.Result.Content.ReadAsStringAsync();
                content.Wait();

                return content.Result;
            }
        }

        /// <summary>
        /// Convert the options to JSON and send them to the host.
        /// </summary>
        public static void OnNewVote(OnNewVoteArgs args, string host)
        {
            PostRequest(JsonConvert.SerializeObject(args.VoteOptionNames), CONTENT_TYPE_JSON, host);
        }

        /// <summary>
        /// Send a simple string that indicates the round is a no voting round.
        /// </summary>
        public static void OnNoVotingRound(string host)
        {
            PostRequest("NoVoting", CONTENT_TYPE_TEXT, host);
        }

        /// <summary>
        /// Request the results of the vote.
        /// </summary>
        public static void OnGetVoteResult(OnGetVoteResultArgs e, string host)
        {
            var req = GetRequest(host + VOTE_RESULT_REQUEST);
            e.ChosenOption = int.Parse(req);
        }
    }
}
