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

namespace Noah.Commands
{
    public class NoahServer : Command
    {
        static NoahServer _instance;
        internal NoahClient Client = null;
        internal string WorkDir;

        private int Port = 0;

        private bool ShowEditor = false;

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

        public LoggerPanel logger { get; private set; }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            try
            {
                if (logger == null) Panels.OpenPanel(LoggerPanel.PanelId);
                logger = Panels.GetPanel<LoggerPanel>(doc);
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine("Error: " + ex.Message);
            }

            GH_RhinoScriptInterface Grasshopper = RhinoApp.GetPlugInObject("Grasshopper") as GH_RhinoScriptInterface;

            if (Grasshopper == null)
            {
                return Result.Cancel;
            }

            GetOption go = null;
            while (true)
            {
                var port = new OptionInteger(Port, 1024, 65535);
                var toggle = new OptionToggle(ShowEditor, "Hide", "Show");

                go = new GetOption();

                go.SetCommandPrompt("Noah Server");
                go.AddOption("Connect");
                go.AddOption("Stop");
                go.AddOption("Observer");
                go.AddOptionInteger("Port", ref port);
                go.AddOptionToggle("Editor", ref toggle);
                go.AddOption("Workspace");

                GetResult result = go.Get();
                if (result != GetResult.Option) break;

                ShowEditor = toggle.CurrentValue;

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

        private void Client_WarningEvent(object sender, string message)
        {
            logger.Warning(message);
        }

        private void Client_ErrorEvent(object sender, string message)
        {
            logger.Error(message);
        }

        private void Client_MessageEvent(object sender, string message)
        {
            logger.Info(message);
        }
    }
}