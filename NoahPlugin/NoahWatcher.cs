using System;
using Rhino;
using Rhino.Commands;
using WebSocketSharp;

namespace NoahPlugin
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
            RhinoApp.WriteLine("Initiating Noah data server!");
            var ws = new WebSocket("ws://localhost:5618");

            ws.OnMessage += (sender, e) =>
                    RhinoApp.WriteLine("Noah: " + e.Data);

            ws.OnError += (sender, e) =>
                RhinoApp.WriteLine("Error: " + e.Message);

            ws.OnOpen += (sender, e) =>
                RhinoApp.WriteLine("Noah data server is open ");

            ws.OnClose += (sender, e) =>
                 RhinoApp.WriteLine("Noah data server is close ");

            ws.Connect();

            ws.Send("This is Noah Watcher");
            return Result.Nothing;
        }
    }
}