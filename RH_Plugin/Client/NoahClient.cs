using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noah.Tasker;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;

namespace Noah.CLient
{
    public class NoahClient
    {
        internal int Port;
        internal string WorkDir;
        internal Guid Guid;
        private WebSocket Client;
        private List<NoahTask> TaskList = new List<NoahTask>();

        private int RetryCnt = 0;
        private int MaxRetry = 5;

        public delegate void EchoHandler(object sender, string message);
        public event EchoHandler MessageEvent;
        public event EchoHandler ErrorEvent;

        public NoahClient(int port, string workDir)
        {
            WorkDir = workDir;
            Port = port;
            Guid = Guid.NewGuid();
            Init();
        }

        public void Connect()
        {
            Client.Connect();

            // Client.Send("{\"route\": \"none\", \"msg\": \"This is Rhino\"}");
        }

        public void Close()
        {
            Client.Close();
        }

        private void Init()
        {
            Client = new WebSocket("ws://localhost:" + Port.ToString() + "/data/server/?platform=Rhino&ID=" + Guid.ToString());

            Client.OnMessage += Socket_OnMessage;
            Client.OnError += Socket_OnError;
            Client.OnOpen += Socket_OnOpen;
            Client.OnClose += Socket_OnClose;
        }

        private void Socket_OnClose(object sender, CloseEventArgs e)
        {
            MessageEvent(this, "Noah Client connecting is closed");
            if (RetryCnt == MaxRetry) Exit();
            Reconnect();
        }

        public async void Exit()
        {
            ErrorEvent(this, "_(:_」∠)_ Could not connect to Noah Client, Rhino will exit in 5 second.");
            // TODO Show confirm box
            await Task.Delay(5000);
            Rhino.RhinoApp.Exit();
        }

        public async void Reconnect()
        {
            while (RetryCnt < MaxRetry)
            {
                ++RetryCnt;

                MessageEvent(this, "Retrying to connect Noah Client " + RetryCnt + " times.");
                Connect();

                if (Client.ReadyState == WebSocketState.Open) return;

                await Task.Delay(100);
            }
        }

        private void Socket_OnOpen(object sender, EventArgs e)
        {
            RetryCnt = 0;
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

                            task.SetWorkspace(WorkDir);

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

                            task.DoneEvent += (sd, id) =>
                            {
                                var obj = new JObject
                                {
                                    ["route"] = "task-end",
                                    ["id"] = id
                                };
                                Client.Send(obj.ToString());
                            };

                            task.InfoEvent += (sd, json) =>
                            {
                                Client.Send(json);
                            };

                            task.Run();

                            break;
                        }
                    case ClientEventType.group:
                        {
                            TaskGroup group = JsonConvert.DeserializeObject<TaskGroup>(eve.data);

                            group.tasks.ForEach(task =>
                            {
                                NoahTask _task = (from t in TaskList
                                                  where Equals(t.ID, task.ID)
                                                  select t).FirstOrDefault();

                                if (_task != null)
                                {
                                    ErrorEvent(this, "This task is already running!");
                                    _task.BringToFront();
                                }

                                TaskList.Add(task);

                                MessageEvent(this, task.ID + " is loaded!");

                                task.Run();
                            });
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
        data,
        group
    }
}
