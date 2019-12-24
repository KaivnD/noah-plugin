using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.Input;
using Rhino.Input.Custom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Cairo;
using Surface = Cairo.Surface;
using Eto.Forms;
using Command = Rhino.Commands.Command;
using Noah.Utils;

namespace Noah.Commands
{
    public class ToEps : Command
    {

        public override string EnglishName
        {
            get { return "ToEps"; }
        }

        private readonly ObjectType[] SupportObjectTypes =
        {
            ObjectType.Curve,
            ObjectType.Brep,
            ObjectType.Annotation
        };

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var dialog = new SelectFolderDialog()
            {
                Title = "保存位置"
            };

            DialogResult res = dialog.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow);

            if (res == DialogResult.Ok)
            {
                GetObject go;

                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择边界");
                go.GeometryFilter = ObjectType.Curve;

                if (go.GetMultiple(1, 0) != GetResult.Object) return Result.Failure;

                try
                {
                    List<string> outputFiles = new List<string>();

                    foreach (var obj in go.Objects())
                    {
                        Curve boundCrv = obj.Curve();
                        if (!boundCrv.IsPlanar() || !boundCrv.IsPolyline() || !boundCrv.IsClosed) return Result.Failure;

                        boundCrv.GetBoundingBox(Plane.WorldXY, out Box bound);

                        string page = obj.Object().Name;

                        if (string.IsNullOrWhiteSpace(page)) continue;
                        

                        page = System.IO.Path.Combine(dialog.Directory, page + ".eps");
                        outputFiles.Add(page);

                        var eps = new EncapsulatedPostScript(bound);

                        var objs = SelectAllObjectsInBound(bound);
                        if (objs.Count == 0) continue;

                        eps.SaveEPS(objs, page);
                    }

                    System.Diagnostics.Process.Start(dialog.Directory);

                }
                catch (Exception ex)
                {
                    RhinoApp.WriteLine(ex.Message);
                } finally
                {
                    doc.Objects.UnselectAll();
                    doc.Views.Redraw();
                }
            }

            return Result.Success;
        }

        private List<GeometryBase> SelectAllObjectsInBound(Box bound)
        {
            List<GeometryBase> objs = new List<GeometryBase>();

            foreach (var obj in RhinoDoc.ActiveDoc.Objects)
            {
                obj.Geometry.GetBoundingBox(Plane.WorldXY, out Box objBox);
                if (!bound.Contains(objBox.Center) ||
                    Equals(objBox, bound)) continue;

                if (!SupportObjectTypes.Contains(obj.ObjectType)) continue;

                if (!bound.X.IncludesInterval(objBox.X) ||
                    !bound.Y.IncludesInterval(objBox.Y)) continue;

                objs.Add(obj.Geometry);
                obj.Select(true);
            }

            return objs;
        }

        /// <summary>
        /// Recursive function to print the contents of an instance definition
        /// </summary>
        protected void DumpInstanceDefinition(InstanceDefinition idef, ref int indent)
        {
            if (null != idef && !idef.IsDeleted)
            {
                const string line = "\u2500";
                const string corner = "\u2514";

                var node = (0 == indent) ? line : corner;
                var str = new string(' ', indent * 2);
                RhinoApp.WriteLine($"{str}{node} Instance definition {idef.Index} = {idef.Name}");

                var idef_object_count = idef.ObjectCount;
                if (idef_object_count > 0)
                {
                    indent++;
                    str = new string(' ', indent * 2);
                    for (var i = 0; i < idef_object_count; i++)
                    {
                        var obj = idef.Object(i);
                        if (null != obj)
                        {
                            if (obj is InstanceObject iref)
                                DumpInstanceDefinition(iref.InstanceDefinition, ref indent);
                            else
                                RhinoApp.WriteLine($"{str}{corner} Object {i} = {obj.ShortDescription(false)}\n");
                        }
                    }
                    indent--;
                }
            }
        }
    }
}
