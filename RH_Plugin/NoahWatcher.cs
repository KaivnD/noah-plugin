using System;
using System.Collections.Generic;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Commands;
using WebSocketSharp;

namespace Noah
{
    public class NoahWatcher : Command
    {
        static NoahWatcher _instance;
        public NoahWatcher()
        {
            _instance = this;
        }

        ///<summary>The only instance of the NoahWatcher command.</summary>
        public static NoahWatcher Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "NoahWatcher"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            //RhinoApp.WriteLine("Initiating Noah data server!");
            //var ws = new WebSocket("ws://localhost:9410");

            //ws.OnMessage += (sender, e) =>
            //        RhinoApp.WriteLine("Noah: " + e.Data);

            //ws.OnError += (sender, e) =>
            //    RhinoApp.WriteLine("Error: " + e.Message);

            //ws.OnOpen += (sender, e) =>
            //    RhinoApp.WriteLine("Noah data server is open ");

            //ws.OnClose += (sender, e) =>
            //     RhinoApp.WriteLine("Noah data server is close ");

            //ws.Connect();

            //ws.Send("This is Noah Watcher");
            return Result.Nothing;
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