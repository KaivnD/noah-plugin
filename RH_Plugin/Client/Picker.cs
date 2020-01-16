using Grasshopper.Kernel.Types;
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

namespace Noah.Client
{
    public static class Picker
    {
        public static List<GH_Curve> PickCurves()
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一条或多条曲线，之后回车确认");
                go.GeometryFilter = ObjectType.Curve | ObjectType.EdgeFilter;

                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<GH_Curve> ghCurveList1 = new List<GH_Curve>();
                Array.ForEach(go.Objects(), (ObjRef obj) => ghCurveList1.Add(new GH_Curve(obj.Curve())));
                if (ghCurveList1.Count == 0) return null;
                return ghCurveList1;
            }
        }

        public static List<GH_Guid> PickText()
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一个或多个，之后回车确认");
                go.GeometryFilter = ObjectType.Curve | ObjectType.EdgeFilter;

                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<GH_Guid> ghCurveList1 = new List<GH_Guid>();
                Array.ForEach(go.Objects(), (ObjRef obj) => ghCurveList1.Add(new GH_Guid(obj.ObjectId)));
                if (ghCurveList1.Count == 0) return null;
                return ghCurveList1;
            }
        }

        public static List<GH_Surface> PickFace()
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一个或多个，之后回车确认");
                go.GeometryFilter = ObjectType.Surface;

                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<GH_Surface> ghCurveList1 = new List<GH_Surface>();
                Array.ForEach(go.Objects(), (ObjRef obj) => ghCurveList1.Add(new GH_Surface(obj.Surface())));
                if (ghCurveList1.Count == 0) return null;
                return ghCurveList1;
            }            
        }

        public static List<GH_Point> PickPoint()
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt("请选择一个或多个，之后回车确认");
                go.GeometryFilter = ObjectType.Point;

                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<GH_Point> ghCurveList1 = new List<GH_Point>();
                Array.ForEach(go.Objects(), (ObjRef obj) => ghCurveList1.Add(new GH_Point(obj.Point().Location)));
                if (ghCurveList1.Count == 0) return null;
                return ghCurveList1;
            }
        }

        public static List<T> Pick<T>(ObjectType filter, string message)
        {
            GetObject go;
            while (true)
            {
                go = new GetObject();
                go.AcceptNothing(true);
                go.AcceptEnterWhenDone(true);
                go.SetCommandPrompt(message);
                go.GeometryFilter = filter;

                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<T> list = new List<T>();
                Array.ForEach(go.Objects(), (ObjRef obj) => 
                {
                    dynamic geo;
                    if (typeof(T).Name == "GH_Point")
                    {
                        geo = obj.Point().Location;
                    } else if (typeof(T).Name == "GH_Curve")
                    {
                        geo = obj.Curve();
                    }
                    else geo = null;

                    if (geo != null)
                    {
                        list.Add(geo);
                    }
                });

                if (list.Count == 0) return null;
                return list;
            }
        }
    }
}
