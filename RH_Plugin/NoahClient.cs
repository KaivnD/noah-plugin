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
        private List<NoahTask> TaskList = new List<NoahTask>();

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

            Client.Send("{\"route\": \"none\", \"msg\": \"This is Rhino\"}");
        }

        public void Close()
        {
            Client.Close();
        }

        private void Init()
        {
            // TODO 每个Rhino客户端需要ID和platform=Rhino, 考虑使用url params
            Client = new WebSocket("ws://localhost:9410/data/server");

            Client.OnMessage += Socket_OnMessage;
            Client.OnError += Socket_OnError;
            Client.OnOpen += Socket_OnOpen;
            Client.OnClose += Socket_OnClose;
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            // TODO 断线重联
            MessageEvent(this, "Noah Client connecting is broken");
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
                    case ClientEventType.task:
                        {
                            NoahTask task = JsonConvert.DeserializeObject<NoahTask>(eve.data);

                            NoahTask _task = (from t in TaskList
                                        where Equals(t.ID, task.ID)
                                        select t).FirstOrDefault();

                            if (_task != null)
                            {
                                ErrorEvent(this, "This task is already running!");
                                _task.BringToFront();
                                break;
                            }

                            TaskList.Add(task);

                            MessageEvent(this, task.ID + " is loaded!");

                            task.ErrorEvent += (sd, msg) =>
                            {
                                MessageEvent(sd, msg);
                            };

                            task.Run();

                            break;
                        }
                    case ClientEventType.message:
                        {
                            MessageEvent(this, eve.data);
                            break;
                        }
                    case ClientEventType.data:
                        {
                            TaskData taskData = JsonConvert.DeserializeObject<TaskData>(eve.data);

                            NoahTask task = (from t in TaskList
                                              where Equals(t.ID, taskData.ID)
                                              select t).FirstOrDefault();

                            if (task == null)
                            {
                                ErrorEvent(this, "This task is not running!");
                                break;
                            }

                            task.SetData(taskData);

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
        public ClientEventType route;
        public string data;
    }

    public enum ClientEventType
    {
        message,
        task,
        data
    }
}
