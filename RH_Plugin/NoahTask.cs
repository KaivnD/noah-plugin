using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Plugin;
using Rhino;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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

        public void Run()
        {
            if (type == TaskType.Grasshopper)
            {
                Thread thread = new Thread(new ThreadStart(LoadGhDocument));
                thread.SetApartmentState(ApartmentState.STA); // 重点
                thread.Start();
            }
        }

        private void LoadGhDocument()
        {
            if (content.type != TaskContentType.file) return;

            string file = content.value;

            if (!File.Exists(file))
            {
                ErrorEvent(this, "Work file is not exist!");
                return;
            }

            Commands.Run_GrasshopperOpen(file);
        }

        private void SetInput()
        {
            GH_Document doc = Instances.ActiveCanvas.Document;
            GH_ClusterInputHook[] hooks = doc.ClusterInputHooks();
            GH_Structure<IGH_Goo> data = new GH_Structure<IGH_Goo>();
            GH_Path path = new GH_Path(0);
            GH_Number a = new GH_Number(30);
            data.Append(a);
            hooks[0].SetPlaceholderData(data);
            hooks[0].ExpireSolution(true);
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
