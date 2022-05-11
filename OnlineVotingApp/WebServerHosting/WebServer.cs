using OnlineVotingApp.Config;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OnlineVotingApp.ChaosPipe;
using EngineIOSharp.Server;
using EngineIOSharp.Server.Client;
using EngineIOSharp.Common.Packet;
using System.Net;
using Newtonsoft.Json;
using System.Threading;

namespace OnlineVotingApp.WebServerHosting
{
    class WebServer
    {
        private EngineIOServer socketServer;
        private HttpListener listener;
        private Thread listenerThread;
        private string htmlFile;
        private string engineIO;
        private readonly bool preLoad = false; //preload the html file into memory, set to false for debugging purposes.
        private readonly string HTML_LOCATION = "./chaosmod/index.html";
        private readonly string ENGINEIO_LOCATION = "./chaosmod/engine.io.js";
        private readonly string ENGINEIO_ONLINE_LOCATION = "/engine.io.js";
        private int[] votes = new int[4];
        private Dictionary<string, bool[]> sockets = new Dictionary<string, bool[]>();
        private bool voteRunning = true;
        private bool multivote = false;
        private IChaosPipeClient pipe;
        private string currentVotingState = "endvote";
        public WebServer(IConfig config, IChaosPipeClient pipe)
        {
            if(!pipe.IsConnected()) return;

            this.pipe = pipe;

            socketServer = new EngineIOServer(new EngineIOServerOption((ushort)(int.Parse(config.PortOnline)+1)));
            Console.WriteLine("Socket server online on port "+socketServer.Option.Port.ToString());

            htmlFile = System.IO.File.ReadAllText(HTML_LOCATION);
            engineIO = System.IO.File.ReadAllText(ENGINEIO_LOCATION);

            socketServer.OnConnection(onSocketConnection);
            socketServer.Start();


            listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:" + config.PortOnline+"/");
            listener.Start();
            listenerThread = new Thread(webServerThread);
            listenerThread.Start();
        }

        private void webServerThread()
        {
            while (listener.IsListening)
            {
                HttpListenerContext context = listener.GetContext();
                HttpListenerRequest request = context.Request;
                // Obtain a response object.
                HttpListenerResponse response = context.Response;
                // Construct a response.

                byte[] buffer;

                if (request.Url.AbsolutePath == ENGINEIO_ONLINE_LOCATION) buffer = System.Text.Encoding.UTF8.GetBytes(engineIO);
                else buffer = System.Text.Encoding.UTF8.GetBytes(htmlFile.Replace("<IP>", request.Url.Host.ToString()).Replace("<PORT>", (request.Url.Port + 1).ToString()));
                // Get a response stream and write the response to it.
                response.ContentLength64 = buffer.Length;
                System.IO.Stream output = response.OutputStream;
                output.Write(buffer, 0, buffer.Length);
                // You must close the output stream.
                output.Close();
            }
        }
        
        private void onSocketConnection(EngineIOSocket socket)
        {
            socket.On(EngineIOSocket.Event.CLOSE, () =>
            {
                socket.Dispose();
            });

            socket.OnMessage((EngineIOPacket packet) =>
            {
                if(packet.IsText && packet.Data.StartsWith("update") && voteRunning)
                {
                    sockets[socket.SID] = JsonConvert.DeserializeObject<bool[]>(packet.Data.Replace("update:", ""));
                    calculateVote();
                    socketServer.Broadcast("update:" + JsonConvert.SerializeObject(votes));
                }

            });

            socket.Send(currentVotingState);
            if (multivote) socket.Send("multivote");
        }

        private void calculateVote()
        {
            for (int i = 0; i < votes.Length; i++)
            {
                votes[i] = 0;
            }
            foreach (bool[] value in sockets.Values)
            {
                for(int i=0; i<votes.Length; i++)
                {
                    if (value[i]) 
                    {
                        votes[i] += value[i] ? 1 : 0;
                        if(!multivote) break;
                    }
                }
            }
        }

        public void OnGetResaults(OnGetVoteResultArgs args)
        {
            calculateVote();
            List<int> currentVotes = new List<int>();
            int highestVote = votes.Max();

            for(int i = 0; i < votes.Length; i++)
            {
                if(votes[i] == highestVote)
                {
                    currentVotes.Add(i);
                }
            }

            args.ChosenOption = currentVotes[currentVotes.Count == 0 ? 0 : new Random().Next(currentVotes.Count)];

            sockets.Clear();
            socketServer.Broadcast("endvote");
            voteRunning = false;
        }

        public void OnNonVotingRound()
        {
            currentVotingState = "nonvote";
            socketServer.Broadcast("nonvote");
        }

        public void OnNewVote(OnNewVoteArgs args)
        {
            currentVotingState = "newvote:" + JsonConvert.SerializeObject(args.VoteOptionNames);
            socketServer.Broadcast("newvote:" + JsonConvert.SerializeObject(args.VoteOptionNames));
            voteRunning = true;
        }


    }
}
