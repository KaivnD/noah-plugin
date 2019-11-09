using System;
using System.IO;
using Grasshopper;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters.Hints;
using Grasshopper.Kernel.Special;
using Grasshopper.Kernel.Types;
using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using Rhino.Input;

namespace NoahPlugin
{
    public class RunGhFile : Command
    {
        static RunGhFile _instance;
        private GH_Document m_document;

        public RunGhFile()
        {
            _instance = this;
        }

        ///<summary>The only instance of the RunGhFile command.</summary>
        public static RunGhFile Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "RunGhFile"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete command.
            string path = "";
            Result getArgs = RhinoGet.GetString("加载文件", true, ref path);
            if (File.Exists(path))
            {
                GH_DocumentIO io = new GH_DocumentIO();
                io.Open(path);
                m_document = io.Document;
                DataTree<object> dataTree = SolutionTrigger();
                using (GH_PreviewUtil previewUtil = new GH_PreviewUtil())
                {
                    foreach (var data in dataTree.AllData())
                    {
                        var obj = data as GH_Point;

                        if (obj != null) previewUtil.AddPoint(new Point3d(obj.Value.X, obj.Value.Y, obj.Value.Z));
                        RhinoApp.WriteLine(string.Format("{0}", data.GetType()));
                    }
                }

            }
            else RhinoApp.WriteLine(string.Format("{0} 文件不存在", path));
            return Result.Success;
        }

        private DataTree<object> SolutionTrigger()
        {
            DataTree<object> dataTree = null;

            GH_Document doc = m_document;
            if (doc == null)
                throw new Exception("File could not be opened.");

            doc.Enabled = true;
            doc.NewSolution(true, GH_SolutionMode.Silent);

            GH_ClusterOutputHook[] outputs = doc.ClusterOutputHooks();

            dataTree = new DataTree<object>();
            var hint = new GH_NullHint();
            dataTree.MergeStructure(outputs[0].VolatileData, hint);

            doc.Dispose();

            return dataTree;
        }
    }
}