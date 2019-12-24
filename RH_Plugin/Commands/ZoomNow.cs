using Grasshopper;
using Grasshopper.Kernel;
using Rhino;
using Rhino.Commands;
using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Commands
{
    public class ZoomNow : Command
    {
        public override string EnglishName => "ZoomNow";

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            Zoom();
            return Result.Success;
        }

        public static void Zoom()
        {
            var canvas = Instances.ActiveCanvas;
            if (canvas == null) return;
            if (!canvas.IsDocument) return;
            var ghDocumentObjectList = canvas.Document.EnabledObjects();
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
                        if (current.Hidden) continue;
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

            foreach (RhinoView view in RhinoDoc.ActiveDoc.Views)
            {
                view.ActiveViewport.ZoomBoundingBox(bbox);
            }
        }
    }
}
