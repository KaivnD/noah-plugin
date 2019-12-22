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

namespace Noah.Commands
{
    public class ToEps : Command
    {

        public override string EnglishName
        {
            get { return "ToEps"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            GetObject go;

            go = new GetObject();
            go.AcceptNothing(true);
            go.AcceptEnterWhenDone(true);
            go.SetCommandPrompt("请选择边界");
            go.GeometryFilter = ObjectType.Curve;

            if (go.Get() != GetResult.Object) return Result.Failure;

            Curve boundCrv = go.Object(0).Curve();

            if (!boundCrv.IsPlanar() || !boundCrv.IsPolyline() || !boundCrv.IsClosed) return Result.Failure;
            
            boundCrv.GetBoundingBox(Plane.WorldXY, out Box bound);
            double width = bound.X.Max - bound.X.Min;
            double height = bound.Y.Max - bound.Y.Min;

            List<Curve> crvs = new List<Curve>();

            foreach (var obj in doc.Objects)
            {
                obj.Geometry.GetBoundingBox(Plane.WorldXY, out Box objBox);
                if (!bound.Contains(objBox.Center) || 
                    !(obj.Geometry is Curve crv) || 
                    !crv.IsPlanar() ||
                    Equals(objBox, bound)) continue;
                
                if (bound.X.IncludesInterval(objBox.X) && bound.Y.IncludesInterval(objBox.Y))
                {
                    crvs.Add(crv);
                    obj.Select(true);
                }
            }

            if (crvs.Count == 0) return Result.Cancel;

            var dialog = new SaveFileDialog()
            {
                Title = "保存位置",
                Filters =
                {
                    new FileFilter("eps file", new string[] {"eps"})
                }
            };

            DialogResult res = dialog.ShowDialog(Rhino.UI.RhinoEtoApp.MainWindow);
            
            if (res == DialogResult.Ok)
            {
               
                string savePath = dialog.FileName + "." + dialog.CurrentFilter.Extensions[0];
                int prograss = 0;
                using (Surface surface = new PSSurface(savePath, width, height))
                {
                    using (var c = new Context(surface))
                    {
                        //c.Translate(bound.X.Min, bound.Y.Min);
                        c.Antialias = Antialias.Subpixel;
                        c.SetSourceColor(new Color(0, 0, 0, 1));

                        c.LineWidth = 0.1;

                        c.Rectangle(0, 0, width, height);
                        c.Stroke();

                        foreach (var crv in crvs)
                        {
                            ++prograss;
                            if (crv.TryGetArc(out Arc arc))
                            {
                                c.Arc(arc.Center.X, height - arc.Center.Y, arc.Radius, arc.StartAngle, arc.EndAngle);
                                c.Stroke();
                            }
                            else if (crv.TryGetCircle(out Circle circle))
                            {
                                c.Arc(circle.Center.X, height - circle.Center.Y, circle.Radius, 0, 2 * Math.PI);
                                c.Stroke();
                            }
                            else if (crv.TryGetEllipse(out Ellipse ellipse))
                            {
                                continue;
                            }
                            else if (crv.TryGetPolyline(out Polyline pts))
                            {
                                var sPt = pts[0];
                                c.MoveTo(sPt.X, height - sPt.Y);
                                pts.ForEach(pt =>
                                {
                                    if (pts.IndexOf(pt) > 0)
                                    {
                                        c.LineTo(pt.X, height - pt.Y);
                                    }
                                });
                                if (pts.IsClosed) c.LineTo(sPt.X, height - sPt.Y);
                                c.Stroke();
                            }
                            else
                            {

                            }                            
                            RhinoApp.WriteLine($"已完成 {(double)prograss / crvs.Count * 100} %");
                        }
                    }
                }

                RhinoApp.WriteLine(savePath);

            }

            doc.Objects.UnselectAll();

            return Result.Success;
        }
    }
}
