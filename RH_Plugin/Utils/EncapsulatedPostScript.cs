using Cairo;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using Surface = Cairo.Surface;

namespace Noah.Utils
{
    public class EncapsulatedPostScript
    {
        public Box Bound;
        private readonly double Width;
        private readonly double Height;

        public EncapsulatedPostScript(Box box)
        {
            Bound = box;

            Width = Bound.X.Max - Bound.X.Min;
            Height = Bound.Y.Max - Bound.Y.Min;
        }

        public void Save(List<RhinoObject> geometries, string path)
        {
            if (!Bound.IsValid) throw new Exception("No Bound Box");
            if (string.IsNullOrEmpty(path)) throw new Exception("Save Path is required");

            using (Surface surface = new PSSurface(path, Width, Height))
            {
                using (var c = new Context(surface))
                {
                    c.Antialias = Antialias.Subpixel;
                    c.SetSourceColor(new Color(0, 0, 0, 1));

                    c.LineWidth = 0.1;

                    c.Rectangle(0, 0, Width, Height);
                    c.Stroke();

                    c.Translate(-Bound.X.Min, Bound.Y.Min);

                    foreach (var obj in geometries)
                    {
                        switch (obj.ObjectType)
                        {
                            case ObjectType.Curve:
                                DrawCurve(c, obj.Geometry as Curve);
                                break;
                            case ObjectType.Point:
                            case ObjectType.Surface:
                            case ObjectType.Mesh:
                            case ObjectType.Hatch:
                            case ObjectType.InstanceReference:
                            case ObjectType.InstanceDefinition:
                            case ObjectType.TextDot:
                            default:
                                break;
                        }
                    }
                }
            }
        }

        private void DrawCurve(Context c, Curve crv)
        {
            if (crv.TryGetArc(out Arc arc))
            {
                c.Arc(arc.Center.X, Height - arc.Center.Y, arc.Radius, arc.StartAngle, arc.EndAngle);
            }
            else if (crv.TryGetCircle(out Circle circle))
            {
                c.Arc(circle.Center.X, Height - circle.Center.Y, circle.Radius, 0, 2 * Math.PI);
            }
            else if (crv.TryGetEllipse(out Ellipse ellipse))
            {
            }
            else if (crv.TryGetPolyline(out Polyline pts))
            {
                var sPt = pts[0];
                c.MoveTo(sPt.X, Height - sPt.Y);
                pts.ForEach(pt =>
                {
                    if (pts.IndexOf(pt) > 0)
                    {
                        c.LineTo(pt.X, Height - pt.Y);
                    }
                });
                if (pts.IsClosed) c.LineTo(sPt.X, Height - sPt.Y);
            }
            else if (crv is PolyCurve polyCurve)
            {

            }
            else
            {

            }
            c.Stroke();
        }
    }
}
