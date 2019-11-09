using System;
using System.IO;
using Rhino;
using Rhino.Commands;
using Rhino.Input;

namespace NoahPlugin
{
    public class Load3dm : Command
    {
        static Load3dm _instance;
        public Load3dm()
        {
            _instance = this;
        }

        ///<summary>The only instance of the Load3dm command.</summary>
        public static Load3dm Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "Load3dm"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            string path = "";
            Result getArgs = RhinoGet.GetString("加载文件", true, ref path);
            if (File.Exists(path))
            {
                bool alreadyOpen = false;
                RhinoDoc.Open(path, out alreadyOpen);
                if (alreadyOpen) RhinoApp.WriteLine(string.Format("已加载 {0}", path));
            } else RhinoApp.WriteLine(string.Format("{0} 文件不存在", path));
            return Result.Success;
        }
    }
}