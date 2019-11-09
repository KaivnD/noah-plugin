using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Noah
{
    public class NoahClient
    {
        internal int Port;
        private WebSocket Client;

        public delegate void EchoHandler(object sender, string message);
        public event EchoHandler MessageEvent;
        public event EchoHandler ErrorEvent;

        public NoahClient(int port)
        {
            Port = port;
            Init();
        }

        public void Connect()
        {
            Client.Connect();
            Client.Send("This is Rhino");
        }

        public void Close()
        {
            Client.Close();
        }

        private void Init()
        {
            Client = new WebSocket("ws://localhost:9410/data/center");

            Client.OnMessage += Socket_OnMessage;
            Client.OnError += Socket_OnError;
            Client.OnOpen += Socket_OnOpen;
            Client.OnClose += Socket_OnClose;
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            MessageEvent(this, "Noah Client is down");
        }

        private void Socket_OnOpen(object sender, EventArgs e)
        {
        }

        private void Socket_OnError(object sender, ErrorEventArgs e)
        {
            ErrorEvent(this, e.Message);
        }

        private void Socket_OnMessage(object sender, MessageEventArgs e)
        {
            try
            {
                ClientEventArgs eve = JsonConvert.DeserializeObject<ClientEventArgs>(e.Data);
                switch (eve.route)
                {
                    case "task":
                        {
                            NoahTask task = JsonConvert.DeserializeObject<NoahTask>(eve.data);

                            task.ErrorEvent += (sd, msg) =>
                            {
                                MessageEvent(sd, msg);
                            };

                            task.Run();

                            break;
                        }
                    case "message":
                        {
                            MessageEvent(this, eve.data);
                            break;
                        }
                    case "data":
                        {
                            MessageEvent(this, eve.data);
                            break;
                        }
                    default:
                        break;
                }
            } catch (Exception ex)
            {
                ErrorEvent(this, ex.Message);
                ErrorEvent(this, e.Data);
            }
        }
    }

    public class ClientEventArgs
    {
        public string route;
        public string data;
    }
}
