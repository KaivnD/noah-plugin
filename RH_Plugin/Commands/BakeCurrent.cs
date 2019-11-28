using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Noah.Tasker;
using Noah.Utils;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Noah.Commands
{
    public class BakeCurrent : Command
    {
        static BakeCurrent _instance;
        public BakeCurrent()
        {
            _instance = this;
        }

        ///<summary>The only instance of the BakeCurrent command.</summary>
        public static BakeCurrent Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "BakeCurrent"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            // TODO: complete BakeCurrent command.
            var canvas = Instances.ActiveCanvas;

            if (canvas == null)
            {
                RhinoApp.WriteLine("Canvas can't be null");
                return Result.Failure;
            }

            var ghDoc = canvas.Document;

            if (ghDoc == null)
            {
                RhinoApp.WriteLine("ghDoc can't be null");
                return Result.Failure;
            }

            var hooks = ghDoc.ClusterOutputHooks();
            if (hooks == null)
            {
                RhinoApp.WriteLine("ghDoc has no output hooks");
                return Result.Failure;
            }

            foreach (var hook in hooks)
            {
                string info = hook.CustomDescription;
                if (string.IsNullOrEmpty(info)) continue;
                var paraMap = NoahTask.ConvertUrlParam(info);

                if (!paraMap.TryGetValue("Index", out string index)
                    || !paraMap.TryGetValue("Type", out string type)) continue;

                var volatileData = hook.VolatileData;

                if (volatileData.IsEmpty) continue;

                if (type != "3DM") continue;

                RhinoApp.WriteLine("开始Bake");

                DocObjectBaker baker = new DocObjectBaker(doc);

                foreach (var data in volatileData.AllData(true))
                {
                    GeometryBase obj = GH_Convert.ToGeometryBase(data);
                    if (obj == null)
                    {
                        RhinoApp.WriteLine(obj.GetType().ToString());
                        continue;
                    }

                    string layer = obj.GetUserString("Layer");
                    if (layer == null) continue;
                    ObjectAttributes att = new ObjectAttributes
                    {
                        LayerIndex = baker.GetLayer(layer, Color.Black)
                    };

                    baker.ObjectMap.Add(att, obj);
                }

                baker.Bake();
            }
            return Result.Success;
        }
    }
}