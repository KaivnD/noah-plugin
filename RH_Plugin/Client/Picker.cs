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
        public static GH_Curve PickCurve()
        {
            GetObject getObject;
            while (true)
            {
                getObject = new GetObject();
                getObject.SetCommandPrompt("Pick a Curve");
                getObject.GeometryFilter = ObjectType.Curve | ObjectType.EdgeFilter;

                if (getObject.Get() != GetResult.Object) continue;

                GH_Curve ghCurve = new GH_Curve(getObject.Object(0).Curve());

                return ghCurve;
            }
        }
    }
}
