using Grasshopper;
using Grasshopper.Plugin;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah
{
    public class NoahTask
    {

        public Guid ID { get; set; }
        public TaskType type { get; set; }
        public TaskContent content { get; set; }

        public delegate void EchoHandler(object sender, string message);

        public event EchoHandler ErrorEvent;

        [STAThread]
        public void Run()
        {
            if (type == TaskType.Grasshopper)
            {
                if (content.type != TaskContentType.file) return;

                string file = content.value;

                if (!File.Exists(file))
                {
                    ErrorEvent(this, "Work file is not exist!");
                    return;
                }

                var Grasshopper = RhinoApp.GetPlugInObject("Grasshopper") as GH_RhinoScriptInterface;

                if (Grasshopper == null)
                {
                    ErrorEvent(this, "Can not get grasshopper");
                    return;
                }

                Grasshopper.DisableBanner();
                Grasshopper.ShowEditor();

            }
        }
        //public List<Data> DataTable { get; set; }
    }

    public enum TaskType
    {
        Grasshopper,
        Python
    }

    public enum TaskContentType
    {
        file,
        @string
    }

    public class TaskContent
    {
        public TaskContentType type { get; set; }
        public string value { set; get; }
    }

    public class Data
    {
        public string ID;
        public string name;
        public string value;
    }

    public interface ITask
    {
        Guid ID { get; }
        TaskType Type { get; }
        TaskContent Content { get; }
        List<Data> DataTable { get; }
    }
}
