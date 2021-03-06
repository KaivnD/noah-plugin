﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noah.Tasker;
using Noah.UI;
using Noah.Utils;
using Eto.Forms;
using Rhino;
using Rhino.UI;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WebSocketSharp;
using Eto.Drawing;
using Grasshopper.Kernel;
using Noah.Client;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Types;
using Grasshopper.GUI.Canvas;
using Grasshopper;
using Rhino.Input;

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

        private bool restoreHistory;

        public event InfoHandler InfoEvent;
        public event ErrorHandler ErrorEvent;
        public event WarningHandler WarningEvent;
        public event DebugHandler DebugEvent;

        private HistoryPanel HistoryPanel { get; set; }

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
            WarningEvent(this, "Noah Client connecting is closed");
            if (RetryCnt == MaxRetry) Exit();
            Reconnect();
        }

        public async void Exit()
        {
            ErrorEvent(this, "_(:_」∠)_ Could not connect to Noah Client, Rhino will exit in 3 second.");
            await Task.Delay(300);

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                DialogResult dialogResult = MessageBox.Show(
                    RhinoEtoApp.MainWindow,
                    "Noah Server 已断线并且重联5次都失败了，是否关闭Rhino",
                    "重联失败",
                    MessageBoxButtons.YesNo,
                    MessageBoxType.Error);
                if (dialogResult == DialogResult.Yes)
                {
                    RhinoApp.Exit();
                }
            }));
        }

        public async void Reconnect()
        {
            while (RetryCnt < MaxRetry)
            {
                ++RetryCnt;

                WarningEvent(this, "Retrying to connect Noah Client " + RetryCnt + " times.");
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
                            TaskRunner(task);

                            break;
                        }
                    case ClientEventType.message:
                        {
                            InfoEvent(this, eve.data);
                            break;
                        }
                    case ClientEventType.data:
                        {
                            TaskData taskData = JsonConvert.DeserializeObject<TaskData>(eve.data);

                            NoahTask noahTask = TaskList.Find(task => Equals(task.ID, taskData.ID));

                            if (noahTask == null)
                            {
                                DebugEvent("This task is not running!");
                                break;
                            }

                            GH_Canvas activeCanvas = Instances.ActiveCanvas;
                            if (activeCanvas == null || !activeCanvas.IsDocument)
                            {
                                ErrorEvent(this, "No Active Canvas exist!");
                                return;
                            }

                            if (activeCanvas.Document.Properties.ProjectFileName != taskData.ID.ToString())
                            {
                                DebugEvent("这个任务没有置于前台!");
                                return;
                            }

                            DebugEvent(taskData.type);
                            noahTask.dataList.Add(taskData);
                            noahTask.dataTable = taskData.table;

                            TaskRunner(noahTask);

                            break;
                        }
                    case ClientEventType.pick:
                        {
                            RhinoApp.InvokeOnUiThread(new Action(() =>
                            {

                                GetOption go = null;
                                var structrue = new GH_Structure<IGH_Goo>();
                                while (true)
                                {
                                    go = new GetOption();

                                    go.SetCommandPrompt("需要选择什么呢？");
                                    go.AddOption(new LocalizeStringPair("Point", "点"));
                                    go.AddOption(new LocalizeStringPair("Curve", "线"));
                                    go.AddOption(new LocalizeStringPair("Surface", "面"));
                                    go.AddOption(new LocalizeStringPair("Guid", "ID"));                                   
                                    

                                    GetResult result = go.Get();
                                    if (result != GetResult.Option) break;

                                    string whereToGo = go.Option().EnglishName;

                                    if (whereToGo == "Curve")
                                    {
                                        var res = Picker.PickCurves();
                                        if (res == null) return;

                                        res.ForEach(crv => structrue.Append(crv));
                                        break;
                                    } else if (whereToGo == "Point")
                                    {
                                        var res = Picker.PickPoint();
                                        if (res == null) return;

                                        res.ForEach(pt => structrue.Append(pt));
                                        break;
                                    } else if (whereToGo == "Surface")
                                    {
                                        var res = Picker.PickFace();
                                        if (res == null) return;

                                        res.ForEach(face => structrue.Append(face));
                                        break;
                                    }
                                    else if (whereToGo == "Guid")
                                    {
                                        var res = Picker.PickGuid();
                                        if (res == null) return;

                                        res.ForEach(text => structrue.Append(text));
                                        break;
                                    } else
                                    {
                                        break;
                                    }
                                }

                                Client.Send(JsonConvert.SerializeObject(new JObject
                                {
                                    ["route"] = "store-picker-data",
                                    ["guid"] = eve.data,
                                    ["bytes"] = IO.SerializeGrasshopperData(structrue)
                                }));

                            }));
                            break;
                        }
                    default:
                        break;
                }
            } catch (Exception ex)
            {
                ErrorEvent(this, ex.Message);
            }
        }

        private void HistoryPanel_RestoreEvent(TaskRow taskRow)
        {
            NoahTask noahTask = TaskList.Find(task => Equals(task.ID, taskRow.TaskID));
            if (noahTask == null) return;

            string[][] taskTable = JsonConvert.DeserializeObject<string[][]>(taskRow.Table);

            List<string> alphaTable = new List<string>();

            int cha = 65;
            int Z = 90;

            while(cha <= Z)
            {
                alphaTable.Add(((char)cha).ToString());
                ++cha;
            }

            List<TaskData> taskDatas = new List<TaskData>();

            for(int i = 0; i < taskTable.GetLength(0); i++)
            {
                for(int j = 0; j < taskTable[i].Length; j++)
                {
                    string dataID = "@" + alphaTable[j] + i;
                    string value = taskTable[i][j];
                    if (string.IsNullOrEmpty(value)) continue;
                    taskDatas.Add(new TaskData
                    {
                        dataID = dataID,
                        name = dataID,
                        ID = noahTask.ID,
                        table = taskRow.Table,
                        value = value
                    });
                }
            }

            taskRow.TaskDatas.ForEach(data => taskDatas.Add(data));

            noahTask.dataList = taskDatas;
            noahTask.dataTable = taskRow.Table;

            TaskRunner(noahTask, true);
        }

        private void TaskRunner (NoahTask task, bool restore = false)
        {
            NoahTask _task = TaskList.Find(__task => Equals(__task.ID, task.ID));

            if (_task != null)
            {
                // ErrorEvent(this, "This task is already running!");
                restoreHistory = false;
                _task.dataList = task.dataList;
                _task.BringToFront(restore);
                return;
            }

            TaskList.Add(task);
            restoreHistory = true;

            string taskInstanceName = string.Format("{0}({1})", task.name, task.ID.ToString().Split('-')[0]);

            InfoEvent(this, taskInstanceName + " 已加载");

            task.ErrorEvent += (sd, msg) => ErrorEvent(sd, msg);
            task.DebugEvent += msg => DebugEvent(msg);

            task.DoneEvent += (sd, id, isRestore) =>
            {
                var obj = new JObject
                {
                    ["route"] = "task-end",
                    ["id"] = id
                };
                Client.Send(obj.ToString());

                if (!(sd is NoahTask noahTask)) return;

                if (noahTask.history.Count < 1)
                {
                    ErrorEvent(this, string.Format("{0} 没有历史", taskInstanceName));
                    return;
                }

                if (!isRestore) RestoreHistoryPanel(noahTask);

                InfoEvent(noahTask, string.Format("{0} " + (isRestore ? "已恢复" : "完成"), taskInstanceName));

            };

            task.StoreEvent += (sd, json) =>
            {
                Client.Send(json);
            };

            task.Run();
        }

        private void RestoreHistoryPanel(NoahTask task, bool endOfTask = true)
        {
            DebugEvent(string.Format("Restoring {0} History", task.name));
            RhinoApp.InvokeOnUiThread(new Action(() => 
            {
                if (HistoryPanel == null) Panels.OpenPanel(HistoryPanel.PanelId);

                HistoryPanel = Panels.GetPanel<HistoryPanel>(RhinoDoc.ActiveDoc);

                if (restoreHistory)
                {
                    if (task.history.Count > 0)
                    {
                        DebugEvent(string.Format("{0} Has {1} History", task.name, task.history.Count));
                        HistoryPanel.SetHistory(task.name, task.history);
                        restoreHistory = false;
                    }
                } else
                {
                    DebugEvent(string.Format("Pushing {0} History", task.name));
                    HistoryPanel.AddHistory(task.name, task.history.Last());
                }

                HistoryPanel.RestoreEvent -= HistoryPanel_RestoreEvent;
                HistoryPanel.RestoreEvent += HistoryPanel_RestoreEvent;

                HistoryPanel.StoreEvent -= HistoryPanel_StoreEvent;
                HistoryPanel.StoreEvent += HistoryPanel_StoreEvent;

                HistoryPanel.DeleteEvent -= HistoryPanel_DeleteEvent;
                HistoryPanel.DeleteEvent += HistoryPanel_DeleteEvent;
            }));
        }

        private void HistoryPanel_DeleteEvent(TaskRow taskRow)
        {
            Client.Send(JsonConvert.SerializeObject(new JObject
            {
                ["route"] = "history-delete",
                ["id"] = taskRow.HistoryID.ToString()
            }));
        }

        private void HistoryPanel_StoreEvent(TaskRow taskRow)
        {
            Client.Send(JsonConvert.SerializeObject(new JObject
            {
                ["route"] = "history-store",
                ["id"] = taskRow.HistoryID.ToString(),
                ["task"] = taskRow.TaskID.ToString(),
                ["title"] = taskRow.title,
                ["memo"] = taskRow.memo,
                ["table"] = taskRow.Table,
                ["taskData"] = JsonConvert.SerializeObject(taskRow.TaskDatas),
                ["thumbnail"] = taskRow.thumbnail.ToByteArray(ImageFormat.Png),
                ["img"] = taskRow.BigBitmap.ToByteArray(ImageFormat.Png)
            }));
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
        group,
        pick
    }
}
