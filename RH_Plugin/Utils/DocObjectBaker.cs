using Rhino;
using Rhino.DocObjects;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Utils
{
    public class DocObjectBaker
    {
        public DocObjectBaker(RhinoDoc doc)
        {
            rhinoDoc = doc;
        }

        public SortedDictionary<string, Guid> LayerMap;
        public Dictionary<ObjectAttributes, GeometryBase> ObjectMap { get; private set; }
        public RhinoDoc rhinoDoc { get; private set; }

        public void Bake()
        {
            foreach (KeyValuePair<ObjectAttributes, GeometryBase> item in ObjectMap)
            {
                var obj = item.Value;
                var attr = item.Key;
                if (obj == null) return;
                switch (obj.ObjectType)
                {
                    case ObjectType.Brep:
                        rhinoDoc.Objects.AddBrep(obj as Brep, attr);
                        break;
                    case ObjectType.Curve:
                        rhinoDoc.Objects.AddCurve(obj as Curve, attr);
                        break;
                    case ObjectType.Point:
                        rhinoDoc.Objects.AddPoint((obj as Rhino.Geometry.Point).Location, attr);
                        break;
                    case ObjectType.Surface:
                        rhinoDoc.Objects.AddSurface(obj as Surface, attr);
                        break;
                    case ObjectType.Mesh:
                        rhinoDoc.Objects.AddMesh(obj as Mesh, attr);
                        break;
                    default:
                        break;
                }
            }
        }

        internal int GetLayer(string layerPath, Color color)
        {
            Guid id;

            // 先根据图层路径执行子图层解决方案
            ChildLayerSolution(layerPath);

            // 从LayerMap中根据layerPath找到ID
            if (!LayerMap.TryGetValue(layerPath, out id))
            {
                // 不存在，需要分情况创建
                if (layerPath.Contains(Layer.PathSeparator))
                {
                    // 这种情况需要先找到父图层，再创建子图层
                    string tmp = layerPath.Replace(Layer.PathSeparator, "-");
                    string[] arr = tmp.Split('-');

                    Guid parentID;

                    if (!LayerMap.TryGetValue(arr[0], out parentID))
                    {
                        return 0;// 这个情况应该不会出现
                    }

                    id = CreateChildLayer(layerPath, color, parentID);
                }
                else id = CreateLayer(layerPath, color);
            }

            Layer layer = rhinoDoc.Layers.FindId(id);

            if (layer == null) return 0;

            return layer.Index;
        }

        public void ChildLayerSolution(string name)
        {
            if (!name.Contains(Layer.PathSeparator)) return;
            string tmp = name.Replace(Layer.PathSeparator, "-");
            string[] arr = tmp.Split('-');

            Guid parentID;

            if (LayerMap.TryGetValue(arr[0], out parentID)) return;

            CreateLayer(arr[0], Color.Black);
        }

        private Guid CreateLayer(string name, Color color)
        {
            Guid id;

            // 从LayerMap中根据layerPath找到ID
            if (LayerMap.TryGetValue(name, out id)) return id;

            id = Guid.NewGuid();
            LayerMap.Add(name, id);

            Layer layer = new Layer
            {
                Name = name,
                Color = color,
                Id = id
            };

            rhinoDoc.Layers.Add(layer);

            return id;
        }

        private Guid CreateChildLayer(string path, Color color, Guid parent)
        {
            Guid id;

            // 从LayerMap中根据layerPath找到ID
            if (LayerMap.TryGetValue(path, out id)) return id;

            id = Guid.NewGuid();
            LayerMap.Add(path, id);

            string tmp = path.Replace(Layer.PathSeparator, "-");
            string[] arr = tmp.Split('-');

            Layer layer = new Layer
            {
                ParentLayerId = parent,
                Name = arr[1],
                Color = color,
                Id = id
            };

            rhinoDoc.Layers.Add(layer);

            return id;
        }
    }
}
