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
                go.SetCommandPrompt("请选择一条或多条曲线");
                go.GeometryFilter = ObjectType.Curve | ObjectType.EdgeFilter;

                if (go.GetMultiple(1, 0) != GetResult.Object) return null;

                List<GH_Curve> ghCurveList1 = new List<GH_Curve>();
                Array.ForEach(go.Objects(), (ObjRef obj) => ghCurveList1.Add(new GH_Curve(obj.Curve())));
                if (ghCurveList1.Count == 0) return null;
                return ghCurveList1;
            }
        }
    }
}
