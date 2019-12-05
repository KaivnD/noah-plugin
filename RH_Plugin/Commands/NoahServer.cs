using System;
using System.Collections.Generic;
using System.Diagnostics;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Plugin;
using Noah.CLient;
using Noah.UI;
using Noah.Utils;
using Rhino;
using Rhino.UI;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using WebSocketSharp;
using Eto.Forms;
using Command = Rhino.Commands.Command;

namespace Noah.Commands
{
    public class NoahServer : Command
    {
        static NoahServer _instance;
        internal NoahClient Client = null;
        internal string WorkDir;

        private int Port = 0;

        private bool ShowEditor = false;
        private bool Debug = false;

        public NoahServer()
        {
            _instance = this;
        }

        ///<summary>The only instance of the NoahWatcher command.</summary>
        public static NoahServer Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "NoahServer"; }
        }

        public LoggerPanel Logger { get; private set; }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                if (Logger == null) Panels.OpenPanel(LoggerPanel.PanelId);
                Logger = Panels.GetPanel<LoggerPanel>(doc);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Error: " + ex.Message);
            }


            if (!(RhinoApp.GetPlugInObject("Grasshopper") is GH_RhinoScriptInterface Grasshopper))
            {
                return Result.Cancel;
            }

            GetOption go = null;
            while (true)
            {
                var port = new OptionInteger(Port, 1024, 65535);
                var toggle = new OptionToggle(ShowEditor, "Hide", "Show");
                var debugger = new OptionToggle(Debug, "Off", "On");

                go = new GetOption();

                go.SetCommandPrompt("Noah Server");
                go.AddOption("Connect");
                go.AddOption("Stop");
                go.AddOption("Observer");
                go.AddOptionInteger("Port", ref port);
                go.AddOptionToggle("Editor", ref toggle);
                go.AddOptionToggle("Debug", ref debugger);
                go.AddOption("Workspace");

                GetResult result = go.Get();
                if (result != GetResult.Option) break;

                ShowEditor = toggle.CurrentValue;
                Debug = debugger.CurrentValue;

                string whereToGo = go.Option().EnglishName;

                if (whereToGo == "Connect")
                {
                    if (Port == 0)
                    {
                        RhinoApp.WriteLine("Please set Port you want to connect!");
                        continue;
                    }

                    if (WorkDir == null)
                    {
                        RhinoApp.WriteLine("Noah can not work without workspace!");
                        continue;
                    }

                    if (Client == null)
                    {
                        try
                        {
                            Grasshopper.DisableBanner();

                            if (!Grasshopper.IsEditorLoaded())
                            {
                                Grasshopper.LoadEditor();
                            }

                            Client = new NoahClient(Port, WorkDir);
                            Client.InfoEvent += Client_MessageEvent;
                            Client.ErrorEvent += Client_ErrorEvent;
                            Client.WarningEvent += Client_WarningEvent;
                            Client.DebugEvent += Client_DebugEvent;
                        }
                        catch (Exception ex)
                        {
                            RhinoApp.WriteLine("Error: " + ex.Message);
                        }

                        Client.Connect();
                    }
                    else Client.Reconnect();



                    if (ShowEditor) Grasshopper.ShowEditor();

                    break;
                }

                if (whereToGo == "Stop")
                {
                    if (Port == 0) continue;

                    if (Client != null) Client.Close();
                    break;
                }

                if (whereToGo == "Workspace")
                {
                    RhinoGet.GetString("Noah Workspace", false, ref WorkDir);
                }

                if (whereToGo == "Observer")
                {
                    if (Port == 0)
                    {
                        RhinoApp.WriteLine("Server connecting need a port!");
                        continue;
                    }

                    Process.Start("http://localhost:" + Port + "/data/center");
                    break;
                }

                if (whereToGo == "Port")
                {
                    Port = port.CurrentValue;
                    RhinoApp.WriteLine("Port is set to " + Port.ToString());
                    continue;
                }

            }

            return Result.Nothing;
        }

        private void Client_DebugEvent(string message)
        {
            if (!Debug) return;
            Logger.Debug(message);
        }

        private void Client_WarningEvent(object sender, string message)
        {
            Logger.Warning(message);
        }

        private void Client_ErrorEvent(object sender, string message)
        {
            Logger.Error(message);
        }

        private void Client_MessageEvent(object sender, string message)
        {
            
            Logger.Info(message);
        }
    }
}