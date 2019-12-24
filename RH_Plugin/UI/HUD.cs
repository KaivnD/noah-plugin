using Rhino.Display;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Noah
{
    public class HUD: DisplayConduit
    {
        protected override void DrawForeground(DrawEventArgs e)
        {
            if (!Equals(e.RhinoDoc.Views.ActiveView.ActiveViewport.Name, e.Viewport.Name))
            {
                return;
            }
            var bounds = e.Viewport.Bounds;
            int height = bounds.Width < 600 ? 100 : 200;
            int width = bounds.Width < 600 ? 200 : 300;
            Size size = new Size(width, height);
            double margin = bounds.Width < 600 ? 10 : 20;
            double padding = bounds.Width < 600 ? 5 : 10;
            
            var pt = new Point2d(bounds.Right - size.Width - margin, bounds.Top + margin);
            var pt2 = new Point2d(pt.X + padding, pt.Y + padding);
            e.Display.Draw2dRectangle(new Rectangle((int)pt.X, (int)pt.Y, size.Width, size.Height), Color.Black, 0, Color.FromArgb(30, Color.Black));
            e.Display.Draw2dText("Hi Noah!", Color.White, pt2, false);
        }
    }
}
