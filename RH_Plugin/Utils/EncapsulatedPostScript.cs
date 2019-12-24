using Cairo;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Text;
using Surface = Cairo.Surface;

namespace Noah.Utils
{
    public class EncapsulatedPostScript
    {
        public Box Bound;
        private readonly string FilePath;
        private readonly double Width;
        private readonly double Height;

        public EncapsulatedPostScript(Box box, string path)
        {
            Bound = box;
            FilePath = path;

            Width = Bound.X.Max - Bound.X.Min;
            Height = Bound.Y.Max - Bound.Y.Min;
        }

        public EncapsulatedPostScript(Box box)
        {
            Bound = box;

            Width = Bound.X.Max - Bound.X.Min;
            Height = Bound.Y.Max - Bound.Y.Min;
        }

        public void SavePDF(SortedDictionary<string, List<GeometryBase>> geometries)
        {
            using (Surface surface = new PdfSurface(FilePath, Width, Height))
            using (var c = new Context(surface))
            {
                DefaultContext(c, false);
                foreach (KeyValuePair<string, List<GeometryBase>> page in geometries)
                {
                    Save(page.Value, c);
                    c.ShowPage();
                }
                surface.Finish();
            }
        }

        public void SaveEPS(List<GeometryBase> geometries, string path)
        {
            using (Surface surface = new PSSurface(path, Width, Height))
            using (var c = new Context(surface))
            {
                DefaultContext(c);
                Save(geometries, c);
                surface.Finish();
            }
        }

        public void SaveEPS(SortedDictionary<string, List<GeometryBase>> geometries)
        {
            foreach (KeyValuePair<string, List<GeometryBase>> page in geometries)
            {
                using (Surface surface = new PSSurface(page.Key, Width, Height))
                using (var c = new Context(surface))
                {
                    DefaultContext(c);
                    Save(page.Value, c);
                    surface.Finish();
                }
            }
        }

        private void Save(List<GeometryBase> geometries, Context c)
        {        

            foreach (var obj in geometries)
            {
                if (obj == null || obj.GetBoundingBox(false).Corner(true, true, true).Z > 0) continue;

                switch (obj.ObjectType)
                {
                    case ObjectType.Curve:
                        DrawCurve(c, obj as Curve);
                        break;
                    case ObjectType.Annotation:
                        DrawAnnotation(c, obj);
                        break;
                    case ObjectType.Brep:
                    case ObjectType.Hatch:
                    case ObjectType.InstanceReference:
                    case ObjectType.InstanceDefinition:
                    default:
                        break;
                }
            }
        }

        private void DefaultContext(Context c, bool drawBound = true)
        {
            c.Antialias = Antialias.Subpixel;
            c.SetSourceColor(new Color(0, 0, 0, 1));

            c.LineWidth = 0.1;

            if (drawBound)
            {
                c.Rectangle(0, 0, Width, Height);
                c.Stroke();
            }

            c.Translate(-Bound.X.Min, Bound.Y.Min);
        }

        private void DrawBrep(Context c, Brep brep)
        {            
            if (brep.Faces.Count > 1 || !brep.Faces[0].IsPlanar()) return;
            // TODO 曲面边界填充
            Curve[] curves = brep.DuplicateEdgeCurves(true);
            double tol = Rhino.RhinoDoc.ActiveDoc.ModelAbsoluteTolerance * 2.1;
            curves = Curve.JoinCurves(curves, tol);
            Array.ForEach(curves, crv => DrawCurve(c, crv, DrawCurveMode.Fill));
        }

        private void DrawAnnotation(Context c, GeometryBase obj)
        {
            if (!(obj is AnnotationBase text)) return;
            Font font = text.Font;
            c.SelectFontFace(font.FamilyName, FontSlant.Normal, FontWeight.Normal);

            text.GetBoundingBox(Plane.WorldXY, out Box box);
            c.SetFontSize(text.TextHeight);
            c.MoveTo(box.X.Min, Height - box.Y.Min);
            c.ShowText(text.PlainText);
        }

        public enum DrawCurveMode
        {
            Stroke,
            Fill,
            Both
        }

        private void DrawCurve(Context c, Curve crv, DrawCurveMode mode = DrawCurveMode.Stroke)
        {
            if (!crv.IsPlanar()) return;

            if (crv.TryGetArc(out Arc arc))
            {
                if(arc.IsCircle)
                {
                    c.Arc(arc.Center.X, Height - arc.Center.Y, arc.Radius, 0, 2 * Math.PI);
                    return;
                }
                Point3d start = arc.StartPoint;
                Point3d center = arc.Center;
                double r = arc.Radius;
                double a;
                if (start.X == center.X)
                {
                    if (start.Y > center.Y)
                    {
                        a = Math.PI / 2;
                    }
                    else
                    {
                        a = (3 * Math.PI) / 2;
                    }
                }
                else a = Math.Atan2(start.Y - center.Y, start.X - center.X);

                double end = a + arc.EndAngle;

                if (end > 2 * Math.PI)
                {
                    end -= 2 * Math.PI;
                } else if (end > 4 * Math.PI)
                {
                    end -= 4 * Math.PI;
                }

                c.ArcNegative(arc.Center.X, Height - arc.Center.Y, arc.Radius, a, end);  
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
                DrawPolyline(c, pts);
            }
            else if (crv is PolyCurve polyCurve)
            {
                Curve[] segments = polyCurve.Explode();
                if (segments == null || segments.Length == 0) return;
                Array.ForEach(segments, seg => DrawCurve(c, seg.DuplicateShallow() as Curve));
            }
            else if (crv is PolylineCurve polyline)
            {
                DrawPolyline(c, polyline.ToPolyline());
            } else
            {

            }

            if (mode == DrawCurveMode.Stroke) c.Stroke();
            else if (mode == DrawCurveMode.Fill) c.Fill();
        }

        public void DrawPolyline(Context c, Polyline pl)
        {
            var sPt = pl[0];
            c.MoveTo(sPt.X, Height - sPt.Y);
            pl.ForEach(pt =>
            {
                if (pl.IndexOf(pt) > 0)
                {
                    c.LineTo(pt.X, Height - pt.Y);
                }
            });
            if (pl.IsClosed) c.LineTo(sPt.X, Height - sPt.Y);
        }

        public static bool MakeCurveSegments(ref List<Curve> cList, Curve crv, bool recursive)
        {
            if (crv is PolyCurve polycurve)
            {
                if (recursive) polycurve.RemoveNesting();
                Curve[] segments = polycurve.Explode();
                if (segments == null || segments.Length == 0) return false;
                if (recursive) foreach (Curve segment in segments) MakeCurveSegments(ref cList, segment, recursive);
                else foreach (Curve segment in segments) cList.Add(segment.DuplicateShallow() as Curve);
                return true;
            }

            if (crv is PolylineCurve polyline)
            {
                if (recursive)
                {
                    for (int i = 0; i < (polyline.PointCount - 1); i++) cList.Add(new LineCurve(polyline.Point(i), polyline.Point(i + 1)));
                }
                else cList.Add(polyline.DuplicateCurve());
                return true;
            }

            if (crv.TryGetPolyline(out Polyline p))
            {
                if (recursive)
                {
                    for (int i = 0; i < (p.Count - 1); i++) cList.Add(new LineCurve(p[i], p[i + 1]));
                }
                else cList.Add(new PolylineCurve(p));
                return true;
            }

            if (crv is LineCurve line) { cList.Add(line.DuplicateCurve()); return true; }

            if (crv is ArcCurve arc) { cList.Add(arc.DuplicateCurve()); return true; }

            NurbsCurve nurbs = crv.ToNurbsCurve();
            if (nurbs == null) return false;

            double t0 = nurbs.Domain.Min; double t1 = nurbs.Domain.Max; 
            int cListCount = cList.Count;

            do
            {
                if (!nurbs.GetNextDiscontinuity(Continuity.C1_locus_continuous, t0, t1, out double t)) break;

                Interval trim = new Interval(t0, t);
                if (trim.Length < 1e-10) { t0 = t; continue; }

                Curve nDC = nurbs.DuplicateCurve();
                nDC = nDC.Trim(trim);
                if (nDC.IsValid) cList.Add(nDC);
                t0 = t;
            }
            while (true);

            if (cList.Count == cListCount) cList.Add(nurbs);
            return true;
        }
    }
}
