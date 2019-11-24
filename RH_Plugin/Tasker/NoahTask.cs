using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Grasshopper.Plugin;
using Newtonsoft.Json.Linq;
using Noah.Utils;
using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
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
        public List<TaskData> results { get; }

        public string workspace { set; get; }

        public string taskspace { set; get; }

        public string ticket { set; get; }

        public delegate void EchoHandler(object sender, string message);

        public event EchoHandler ErrorEvent;
        public event EchoHandler DoneEvent;
        public event EchoHandler InfoEvent;

        public void Run()
        {
            if (type == TaskType.Grasshopper)
            {
                RhinoApp.InvokeOnUiThread(new Action(() => { LoadGhDocument(); }));
                //Platform platform = SystemPlatform.Get();
                //if (platform == Platform.Windows)
                //{
                //    Thread thread = new Thread(new ThreadStart(LoadGhDocument));
                //    thread.SetApartmentState(ApartmentState.STA); // 重点
                //    thread.Start();
                //}
                //if (platform == Platform.Mac)
                //{
                //    RhinoApp.InvokeOnUiThread(new Action(() => { LoadGhDocument(); }));
                //}
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

            // SolutionEndCnt = 0;
            doc.SolutionEnd += Doc_SolutionEnd;
            UpdateData(true);
        }

        internal void SetWorkspace(string workDir)
        {
            workspace = workDir;
            taskspace = Path.Combine(workspace, ".noah", "tasks");
        }

        private void Doc_SolutionEnd(object sender, GH_SolutionEventArgs e)
        {
            try
            {
                DoneEvent(sender, ID.ToString());

                StoreOutput();
            } catch (Exception ex)
            {
                ErrorEvent(sender, ex.Message);
            }
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

            // SolutionEndCnt = 0;
            UpdateData(true);
        }

        internal void SetData(TaskData taskData)
        {

            if (!Equals(taskData.ID, ID)) return;

            TaskData match = (from data in dataList
                              where Equals(data.dataID, taskData.dataID)
                              select data).FirstOrDefault();

            if (match != null)
            {
                // if (match.value == taskData.value) return;

                match.value = taskData.value;
                match.name = taskData.name;
            } else dataList.Add(taskData);

            UpdateData(false);
        }

        private void UpdateData(bool recomputeOnTheEnd)
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

                // if (Equals(hook.VolatileData, m_data)) continue;

                hook.SetPlaceholderData(m_data);

                if (!recomputeOnTheEnd) hook.ExpireSolution(true);
            }

            // for data placeholder inside cluster (deep = 1)

            var clusters = new List<GH_Cluster>();
            foreach (var obj in doc.Objects)
            {
                if (!(obj is GH_Cluster cluster)) continue;
                clusters.Add(cluster);
            }

            if (clusters.Count == 0) return;


            foreach (var cluster in clusters)
            {
                foreach (var obj in cluster.Document("").Objects)
                {
                    if (!(obj is IGH_Param param)) continue;

                    string nickname = param.NickName;

                    if (!nickname.StartsWith("@", StringComparison.Ordinal)) continue;

                    TaskData data = dataList.Find(x => x.name == nickname || x.dataID == nickname);

                    if (data == null) continue;

                    Utility.InvokeMethod(param, "Script_ClearPersistentData");
                    Utility.InvokeMethod(param, "Script_AddPersistentData", new List<object>() { data.value });

                    if (!recomputeOnTheEnd) param.ExpireSolution(true);
                    if (!recomputeOnTheEnd) cluster.ExpireSolution(true);
                }
            }

            if (recomputeOnTheEnd) doc.NewSolution(true);

            GH_Canvas activeCanvas = Instances.ActiveCanvas;
            if (activeCanvas == null)
            {
                ErrorEvent(this, "No Active Canvas exist!");
                return;
            }

            activeCanvas.Document.IsModified = false;
            activeCanvas.Refresh();

            dataList.Clear();
        }

        private GH_Structure<IGH_Goo> SingleDataStructrue(object value)
        {
            GH_Structure<IGH_Goo> m_data = new GH_Structure<IGH_Goo>();

            if (value is string path)
            {
                if (File.Exists(path) && Path.GetExtension(path) == ".noahdata")
                {
                    byte[] array;
                    try
                    {
                        array = File.ReadAllBytes(path);
                    }
                    catch
                    {
                        return null;
                    }
                    GH_LooseChunk val = new GH_LooseChunk("Grasshopper Data");
                    val.Deserialize_Binary(array);
                    if (val.ItemCount == 0)
                    {
                        return null;
                    }

                    GH_Structure<IGH_Goo> gH_Structure = new GH_Structure<IGH_Goo>();
                    GH_IReader val2 = val.FindChunk("Block", 0);

                    bool boolean = val2.GetBoolean("Empty");

                    if (boolean) return null;

                    GH_IReader val3 = val2.FindChunk("Data");
                    if (val3 == null)
                    {
                        return null;
                    }
                    else if (!gH_Structure.Read(val3))
                    {
                        return null;
                    }
                    
                    return gH_Structure;
                }
            }

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

        private SortedDictionary<string, string> ConvertUrlParam(string param)
        {
            SortedDictionary<string, string> configMap = new SortedDictionary<string, string>();

            string[] configs = param.Split('&');
            configs = configs.Where(s => !string.IsNullOrEmpty(s)).ToArray();

            foreach (string config in configs)
            {
                string[] args = config.Split('=');
                if (args.Length != 2)
                    continue;
                if (!configMap.ContainsKey(args[0]))
                {
                    configMap.Add(args[0], args[1]);
                }
            }
            return configMap;
        }

        private void StoreOutput()
        {
            GH_DocumentServer doc_server = Instances.DocumentServer;

            if (doc_server == null)
            {
                ErrorEvent(this, "No Document Server exist!");
                return;
            }

            GH_Document doc = doc_server.ToList().Find(x => x.Properties.ProjectFileName == ID.ToString());

            if (doc == null) return;

            var hooks = doc.ClusterOutputHooks();

            string outDir = Path.Combine(taskspace, ID.ToString(), ticket);

            foreach (var hook in hooks)
            {
                string info = hook.CustomDescription;
                var paraMap = ConvertUrlParam(info);
                var volatileData = hook.VolatileData;
                if (paraMap.TryGetValue("Index", out string index)
                    && paraMap.TryGetValue("Type", out string type))
                {
                    string fileName = Path.Combine(outDir, index);
                    switch (type)
                    {
                        case "CSV":
                            {
                                var allData = volatileData.AllData(true);
                                List<string> sList = new List<string>();
                                allData.ToList().ForEach(el =>
                                {
                                    GH_Convert.ToString(el, out string tmp, GH_Conversion.Both);
                                    sList.Add(tmp);
                                });

                                string csv = string.Join(Environment.NewLine, sList);

                                fileName += ".csv";
                                File.WriteAllText(fileName, csv);

                                break;
                            }
                        case "3DM":
                            {
                                fileName += ".3dm";
                                ErrorEvent(this, fileName);

                                File3dmWriter writer = new File3dmWriter(fileName);

                                foreach (var data in volatileData.AllData(true))
                                {
                                    GeometryBase obj = GH_Convert.ToGeometryBase(data);
                                    if (obj == null)
                                    {
                                        ErrorEvent(this, data.TypeName);
                                        continue;
                                    }

                                    string layer = obj.GetUserString("Layer");
                                    if (layer == null) continue;
                                    ObjectAttributes att = new ObjectAttributes
                                    {
                                        LayerIndex = writer.GetLayer(layer, Color.Black)
                                    };

                                    writer.ObjectMap.Add(att, obj);
                                }

                                writer.Write();

                                break;
                            }
                        case "Data":
                            {
                                GH_LooseChunk ghLooseChunk = new GH_LooseChunk("Grasshopper Data");
                                ghLooseChunk.SetGuid("OriginId", this.ID);

                                GH_IWriter chunk = ghLooseChunk.CreateChunk("Block", 0);
                                chunk.SetString("Name", hook.CustomNickName);
                                chunk.SetBoolean("Empty", volatileData.IsEmpty);
                                if (!volatileData.IsEmpty)
                                {
                                    GH_Structure<IGH_Goo> tree = volatileData as GH_Structure<IGH_Goo>;

                                    if (!tree.Write(chunk.CreateChunk("Data")))
                                        ErrorEvent(ghLooseChunk, string.Format("There was a problem writing the {0} data.", (object)hook.CustomNickName));
                                }

                                byte[] bytes = ghLooseChunk.Serialize_Binary();

                                fileName += ".noahdata";
                                File.WriteAllBytes(fileName, bytes);

                                break;
                            }
                        default:
                            break;
                    }

                    InfoEvent(this, new JObject
                    {
                        ["route"] = "task-stored",
                        ["id"] = ID.ToString(),
                        ["index"] = index.ToString(),
                        ["path"] = fileName
                    }.ToString());
                }

            }
        }
    }
}
