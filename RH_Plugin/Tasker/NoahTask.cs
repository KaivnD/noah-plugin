using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
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

namespace Noah.Tasker
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

            activeCanvas.Document = doc;
            activeCanvas.Document.IsModified = false;
            activeCanvas.Refresh();
            doc.NewSolution(false);

            UpdateData();
        }

        public void BringToFront()
        {
            GH_DocumentServer doc_server = Instances.DocumentServer;

            if (doc_server == null)
            {
                ErrorEvent(this, "No Document Server exist!");
                return;
            }

            GH_Document doc = doc_server.ToList().Find(x => x.Properties.ProjectFileName == ID.ToString());

            if (doc == null) return;

            GH_Canvas activeCanvas = Instances.ActiveCanvas;
            if (activeCanvas == null)
            {
                ErrorEvent(this, "No Active Canvas exist!");
                return;
            }

            activeCanvas.Document = doc;
            activeCanvas.Refresh();
            doc.NewSolution(false);

            UpdateData();
        }

        internal void SetData(TaskData taskData)
        {

            if (!Equals(taskData.ID, ID)) return;

            TaskData match = (from data in dataList
                              where Equals(data.dataID, taskData.dataID)
                              select data).FirstOrDefault();

            if (match != null)
            {
                if (match.value == taskData.value) return;

                match.value = taskData.value;
                match.name = taskData.name;
            } else dataList.Add(taskData);

            UpdateData();

            dataList.Clear();
        }

        private void UpdateData()
        {
            if (dataList.Count == 0 || dataList == null) return;

            GH_DocumentServer doc_server = Instances.DocumentServer;

            if (doc_server == null)
            {
                ErrorEvent(this, "No Document Server exist!");
                return;
            }

            GH_Document doc = doc_server.ToList().Find(x => x.Properties.ProjectFileName == ID.ToString());

            if (doc == null) return;

            var hooks = doc.ClusterInputHooks();

            foreach (var hook in hooks)
            {
                TaskData data = dataList.Find(x => x.name == hook.CustomNickName || x.dataID == hook.CustomNickName);

                if (data == null) continue;

                var m_data = SingleDataStructrue(data.value);

                if (Equals(hook.VolatileData, m_data)) continue;

                hook.SetPlaceholderData(m_data);
                hook.ExpireSolution(true);
            }

            // for data placeholder inside cluster (deep = 1)

            var clusters = new List<GH_Cluster>();
            foreach (var obj in doc.Objects)
            {
                var cluster = obj as GH_Cluster;
                if (cluster == null) continue;
                clusters.Add(cluster);
            }

            if (clusters.Count == 0) return;


            foreach (var cluster in clusters)
            {
                foreach (var obj in cluster.Document("").Objects)
                {
                    var param = obj as IGH_Param;
                    if (param == null) continue;

                    string nickname = param.NickName;

                    if (!nickname.StartsWith("@")) continue;

                    TaskData data = dataList.Find(x => x.name == nickname || x.dataID == nickname);

                    if (data == null) continue;

                    Utility.InvokeMethod(param, "Script_ClearPersistentData");
                    Utility.InvokeMethod(param, "Script_AddPersistentData", new List<object>() { data.value });

                    param.ExpireSolution(true);
                    cluster.ExpireSolution(true);
                }
            }
        }

        private GH_Structure<IGH_Goo> SingleDataStructrue(object value)
        {
            GH_Structure<IGH_Goo> m_data = new GH_Structure<IGH_Goo>();

            GH_Number castNumber = null;
            GH_String castString = null;
            if (GH_Convert.ToGHNumber(value, GH_Conversion.Both, ref castNumber))
            {
                m_data.Append(new GH_ObjectWrapper(castNumber));
            }
            else if (GH_Convert.ToGHString(value, GH_Conversion.Both, ref castString))
            {
                m_data.Append(new GH_ObjectWrapper(castString));
            }
            else
            {
                m_data.Append((IGH_Goo)value);
            }

            return m_data;
        }

        private void StoreOutput()
        {
            GH_LooseChunk ghLooseChunk = new GH_LooseChunk("Grasshopper Data");
            ghLooseChunk.SetGuid("OriginId", this.ID);

            //IGH_Param ghParam = Params.Input[index];
            //IGH_Structure volatileData = ghParam.VolatileData;
            //if (string.IsNullOrEmpty(ghParam.NickName))
            //{
            //    ErrorEvent(ghLooseChunk, "Parameters without a name will not be included.");
            //}
            //else
            //{
            //    GH_IWriter chunk = ghLooseChunk.CreateChunk("Block", index);
            //    chunk.SetString("Name", ghParam.NickName);
            //    chunk.SetBoolean("Empty", volatileData.IsEmpty);
            //    if (!volatileData.IsEmpty)
            //    {
            //        GH_Structure<IGH_Goo> tree;
            //        // access.GetDataTree<IGH_Goo>(index, out tree);
            //        if (!tree.Write(chunk.CreateChunk("Data")))
            //            ErrorEvent(ghLooseChunk, string.Format("There was a problem writing the {0} data.", (object)ghParam.NickName));
            //    }
            //}
            byte[] bytes = ghLooseChunk.Serialize_Binary();
        }
    }
}
