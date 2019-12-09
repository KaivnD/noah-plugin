using GH_IO.Serialization;
using Grasshopper;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noah.Utils;
using Rhino;
using Rhino.Display;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
namespace Noah.Tasker
{
    public class NoahTask
    {
        public string name { get; set; }
        public Guid ID { get; set; }
        public TaskType type { get; set; }
        public TaskContent content { get; set; }
        public List<TaskData> dataList { get; set; }
        public List<TaskData> results { get; }

        public List<TaskRecord> history { set; get; }

        public string workspace { set; get; }

        public string taskspace { set; get; }

        public string ticket { set; get; }

        /// <summary>
        /// 每一次执行任务表格部分的参数镜像
        /// </summary>
        public string dataTable { set; get; }

        public delegate void TaskDoneHandler(object sender, string message, bool restore = false);

        private bool IsTaskRestore;

        public event ErrorHandler ErrorEvent;
        public event TaskDoneHandler DoneEvent;
        public event WarningHandler WarningEvent;
        public event InfoHandler StoreEvent;

        public NoahTask()
        {
            history = new List<TaskRecord>();
        }

        public void Run()
        {
            if (type == TaskType.Grasshopper)
            {
                RhinoApp.InvokeOnUiThread(new Action(() => { LoadGhDocument(); }));
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
                StoreOutput();
                Zoom();
            } catch(Exception ex)
            {
                ErrorEvent(sender, ex.Message);
            } finally
            {
                DoneEvent(this, ID.ToString(), IsTaskRestore);
                IsTaskRestore = false;
            }
        }

        private void Zoom()
        {
            var canvas = Instances.ActiveCanvas;
            if (canvas == null) return;
            if (!canvas.IsDocument) return;
            List<IGH_DocumentObject> ghDocumentObjectList = canvas.Document.EnabledObjects();
            if (ghDocumentObjectList == null || ghDocumentObjectList.Count == 0)
                return;
            BoundingBox bbox = BoundingBox.Empty;
            List<IGH_DocumentObject>.Enumerator enumerator1 = ghDocumentObjectList.GetEnumerator();
            try
            {
                while (enumerator1.MoveNext())
                {
                    if (enumerator1.Current is IGH_PreviewObject current)
                    {
                        BoundingBox clippingBox = current.ClippingBox;
                        if (clippingBox.IsValid)
                            bbox.Union(clippingBox);
                    }
                }
            }
            finally
            {
                enumerator1.Dispose();
            }
            if (!bbox.IsValid)
                return;

            foreach(RhinoView view in RhinoDoc.ActiveDoc.Views)
            {
                view.ActiveViewport.ZoomBoundingBox(bbox);
            }
        }

        public void BringToFront(bool restore = false)
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

            IsTaskRestore = restore;

            // SolutionEndCnt = 0;
            UpdateData(true, restore);
        }

        internal void SetData(TaskData taskData)
        {
            // 保证每一次传输过来的数据都是新鲜的
            if (!Equals(taskData.ID, ID) || dataList.Count > 0) return;

            dataTable = taskData.table;

            dataList.Add(taskData);

            UpdateData(false);
        }

        private void UpdateData(bool recomputeOnTheEnd, bool restore = false)
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

            if (!restore)
            {
                var rndData = dataList.FindAll(data => data.type == "4" || data.type == "5");

                history.Add(new TaskRecord
                {
                    name = name,
                    TaskID = ID,
                    HistoryID = Guid.NewGuid(),
                    date = DateTime.Now,
                    table = dataTable,
                    taskDatas = JsonConvert.SerializeObject(rndData)
                });
            }

            foreach (var hook in hooks)
            {
                TaskData data = dataList.Find(x => x.name == hook.CustomNickName || x.dataID == hook.CustomNickName);

                if (data == null) continue;

                GH_Structure<IGH_Goo> m_data;

                if (data.type == "5")
                {
                    m_data = IO.DeserializeGrasshopperData(Convert.FromBase64String((string)data.value));
                } else
                {
                    m_data = SingleDataStructrue(data.value);
                }

                hook.ClearPlaceholderData();

                // if (Equals(hook.VolatileData, m_data)) continue;
                if (!m_data.IsEmpty) hook.SetPlaceholderData(m_data);

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

                    return IO.DeserializeGrasshopperData(array);
                }
            }

            GH_Structure<IGH_Goo> m_data = new GH_Structure<IGH_Goo>();

            GH_Number castNumber = null;
            GH_String castString = null;
            GH_Curve castCurve = null;
            if (GH_Convert.ToGHCurve(value, GH_Conversion.Both, ref castCurve))
            {
                m_data.Append(new GH_ObjectWrapper(castCurve));
            }
            else if (GH_Convert.ToGHNumber(value, GH_Conversion.Both, ref castNumber))
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

        public static SortedDictionary<string, string> ConvertUrlParam(string param)
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

            if (doc_server == null) throw new Exception("No Document Server exist!");

            GH_Document doc = doc_server.ToList().Find(x => x.Properties.ProjectFileName == ID.ToString());

            if (doc == null) throw new Exception("Tasker 未找到GH_Document");

            if (string.IsNullOrEmpty(workspace) || string.IsNullOrEmpty(ticket)) throw new Exception("工作目录和Ticket为空");

            string outDir = Path.Combine(taskspace, ID.ToString(), ticket);

            var hooks = doc.ClusterOutputHooks();
            if (hooks == null) return;

            foreach (var hook in hooks)
            {
                string info = hook.CustomDescription;
                if (string.IsNullOrEmpty(info)) continue;
                var paraMap = ConvertUrlParam(info);

                if (!paraMap.TryGetValue("Index", out string index)
                    || !paraMap.TryGetValue("Type", out string type)) continue;

                string fileName = Path.Combine(outDir, index + "@" + DateTime.Now.ToString("HH-mm-ss MM-dd"));

                var volatileData = hook.VolatileData;

                if (volatileData.IsEmpty) continue;

                dynamic content = null;

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
                            File.WriteAllText(fileName, csv, Encoding.UTF8);
                            content = fileName;
                            break;
                        }
                    case "3DM":
                        {
                            fileName += ".3dm";

                            File3dmWriter writer = new File3dmWriter(fileName);

                            foreach (var data in volatileData.AllData(true))
                            {
                                GeometryBase obj = GH_Convert.ToGeometryBase(data);
                                if (obj == null)
                                {
                                    WarningEvent(this, data.TypeName + "不能转换成GeometryBase");
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
                            content = fileName;
                            break;
                        }
                    case "Data":
                        {
                            try
                            {
                                GH_Structure<IGH_Goo> tree = volatileData as GH_Structure<IGH_Goo>;

                                content = IO.SerializeGrasshopperData(tree, hook.CustomName, volatileData.IsEmpty);

                            } catch(Exception ex)
                            {
                                ErrorEvent(this, ex.Message);
                            }

                            break;
                        }
                    default:
                        break;
                }

                StoreEvent(this, new JObject
                {
                    ["route"] = "task-stored",
                    ["id"] = ID.ToString(),
                    ["index"] = index.ToString(),
                    ["type"] = type,
                    ["content"] = content
                }.ToString());
            }
        }
    }
}
