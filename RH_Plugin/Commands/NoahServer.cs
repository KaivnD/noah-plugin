using System;
using System.Collections.Generic;
using System.Diagnostics;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Plugin;
using Noah.CLient;
using Noah.Utils;
using Rhino;
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

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var Grasshopper = RhinoApp.GetPlugInObject("Grasshopper") as GH_RhinoScriptInterface;

            if (Grasshopper == null)
            {
                return Result.Cancel;
            }

            Grasshopper.DisableBanner();

            if (!Grasshopper.IsEditorLoaded())
            {
                Grasshopper.LoadEditor();
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

                    if (Client == null)
                    {
                        try
                        {
                            Client = new NoahClient(Port);
                            Client.MessageEvent += Client_MessageEvent;
                            Client.ErrorEvent += Client_ErrorEvent;
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

        private void Client_ErrorEvent(object sender, string message)
        {
            RhinoApp.WriteLine("Error: " + message);
        }

        private void Client_MessageEvent(object sender, string message)
        {
            RhinoApp.WriteLine(message);
        }
    }
}