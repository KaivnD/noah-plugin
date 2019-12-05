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
            GetObject getObject;
            while (true)
            {
                getObject = new GetObject();
                getObject.SetCommandPrompt("Pick a Curve");
                getObject.GeometryFilter = ObjectType.Curve | ObjectType.EdgeFilter;

                if (getObject.GetMultiple(1, 0) != GetResult.Object) continue;

                List<GH_Curve> ghCurveList1 = new List<GH_Curve>();
                Array.ForEach(getObject.Objects(), (ObjRef obj) => ghCurveList1.Add(new GH_Curve(obj.Curve())));
                return ghCurveList1;
            }
        }
    }
}
