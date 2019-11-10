using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Plugin;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Input;
using Rhino.Input.Custom;
using WebSocketSharp;

namespace Noah
{
    public class NoahServer : Command
    {
        static NoahServer _instance;
        internal NoahClient Client = null;
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

            // TODO 增加选项，让Noah客户端决定是否开启GH的ui
            bool show = false;
            if (show) Grasshopper.ShowEditor();

            GetOption opts = new GetOption();
            opts.SetCommandPrompt("Select action you want: ");
            opts.AddOption("Start");
            opts.AddOption("Stop");
            GetResult getResult = opts.Get();
            if (getResult == GetResult.Option)
            {
                string whereToGo = opts.Option().EnglishName;
                switch(whereToGo)
                {
                    case "Start":
                        {
                            if (Client == null)
                            {
                                Client = new NoahClient(9410);
                                Client.MessageEvent += Client_MessageEvent;
                                Client.ErrorEvent += Client_ErrorEvent;
                            }

                            Client.Connect();
                            break;
                        }
                    case "Stop":
                        {
                            if (Client != null) Client.Close();
                            break;
                        }
                    default:
                        break;
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

        private void SetParam(string name)
        {
            GH_Canvas canvas = Instances.ActiveCanvas;

            if (canvas != null)
            {
                RhinoApp.WriteLine("Found Grasshopper Instances!");
                GH_Document gH_Document = canvas.Document;
                var objs = gH_Document.FindObjects(new List<string> { "get" }, 20);
                if (objs.Count > 0)
                {
                    foreach (IGH_DocumentObject obj in objs)
                    {
                        if (obj.NickName != name) continue;
                        
                    }
                }
            }
            else
            {
                RhinoApp.WriteLine("Not found Grasshopper Instances!");
            }
        }
    }
}