using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Utils
{
    public class ObjectLayerInfo
    {
        public object Geometry { set; get; }
        public string Name { set; get; }
        public Color Color { set; get; }
        public ObjectLayerInfo(object geometry, string name, Color color)
        {
            Geometry = geometry;
            Name = name;
            Color = color;
        }

        public override string ToString()
        {
            return string.Format("Name: {0}; Color: {1}; Geometry: {2}", Name, Color.ToString(), Geometry.ToString());
        }
    }
}
