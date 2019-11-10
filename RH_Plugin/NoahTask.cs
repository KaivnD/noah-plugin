using Grasshopper;
using Grasshopper.GUI.Canvas;
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
        public List<TaskData> dataList { get; set; }

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
            GH_DocumentIO io = new GH_DocumentIO();
            io.Open(file);

            GH_Document doc = GH_Document.DuplicateDocument(io.Document);

            if (doc == null)
            {
                ErrorEvent(this, "Cannot read this file!");
                return;
            }

            GH_DocumentServer server = Instances.DocumentServer;

            if (server == null)
            {
                ErrorEvent(this, "No Document Server exist!");
                return;
            }

            server.AddDocument(doc);

            doc.Properties.ProjectFileName = ID.ToString();

            GH_Canvas activeCanvas = Instances.ActiveCanvas;
            if (activeCanvas == null)
            {
                ErrorEvent(this, "No Active Canvas exist!");
                return;
            }

            SetInput(doc);

            activeCanvas.Document = doc;
            activeCanvas.Document.IsModified = false;
            activeCanvas.Refresh();
            doc.NewSolution(false);
        }

        public void BringToFront()
        {
            // TODO 将相关Task GH的文档前置
        }

        private void SetInput(GH_Document doc)
        {
            GH_ClusterInputHook[] hooks = doc.ClusterInputHooks();
            GH_Structure<IGH_Goo> data = new GH_Structure<IGH_Goo>();
            GH_Path path = new GH_Path(0);
            GH_Number a = new GH_Number(30);
            data.Append(a);
            hooks[0].SetPlaceholderData(data);
            hooks[0].ExpireSolution(true);
        }

        internal void SetData(TaskData taskData)
        {
            // TODO If taskData ID and dataID match some data inside datalist 
            // then update else add a brand new data to it

            UpdateData();
        }

        private void UpdateData()
        {
            if (dataList.Count == 0 || dataList == null) return;

            foreach (TaskData data in dataList)
            {
                // TODO update data to gh document
            }
        }
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

    public class TaskData
    {
        public Guid ID;
        public string dataID;
        public string name;
        public string value;
    }
}
