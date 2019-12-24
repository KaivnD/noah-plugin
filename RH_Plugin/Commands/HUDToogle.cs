using Rhino;
using Rhino.Commands;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rhino.Display;

namespace Noah.Commands
{
    public class HUDToogle : Command
    {
        public override string EnglishName => "HUDToogle";
        readonly static HUD hud = new HUD();


        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            hud.Enabled = !hud.Enabled;
            doc.Views.Redraw();

            RhinoView.SetActive -= RhinoView_SetActive;
            RhinoView.SetActive += RhinoView_SetActive;
            
            return Result.Success;
        }

        private void RhinoView_SetActive(object sender, ViewEventArgs e)
        {
            if (!hud.Enabled) return;
            hud.Enabled = false;
            hud.Enabled = true;
            RhinoDoc.ActiveDoc.Views.Redraw();
        }
    }
}
