using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace Noah.Utils
{
    public class File3dmWriter
    {
        public string m_path { set; get; }
        public File3dm m_file { set; get; }

        public SortedDictionary<string, Guid> LayerMap;
        public Dictionary<ObjectAttributes, GeometryBase> ObjectMap;

        public File3dmWriter(string path)
        {
            LayerMap = new SortedDictionary<string, Guid>();
            ObjectMap = new Dictionary<ObjectAttributes, GeometryBase>();
            if (File.Exists(path))
            {
                try
                {
                    m_file = File3dm.Read(path);
                    m_file.Objects.Clear();
                    m_file.AllLayers.Clear();
                }
                catch
                {
                    throw new Exception("Cant read this file!");
                }
            }
            else
            {
                m_file = new File3dm();
            }

            m_path = path;
        }

        public void ChildLayerSolution(string name)
        {
            if (!name.Contains(Layer.PathSeparator)) return;
            string tmp = name.Replace(Layer.PathSeparator, "-");
            string[] arr = tmp.Split('-');
            if (LayerMap.TryGetValue(arr[0], out _)) return;

            CreateLayer(arr[0], Color.Black);
        }

        private Guid CreateLayer(string name, Color color)
        {
            // 从LayerMap中根据layerPath找到ID
            if (LayerMap.TryGetValue(name, out Guid id)) return id;

            id = Guid.NewGuid();
            LayerMap.Add(name, id);

            Layer layer = new Layer
            {
                Name = name,
                Color = color,
                Id = id
            };

            m_file.AllLayers.Add(layer);

            return id;
        }

        private Guid CreateChildLayer(string path, Color color, Guid parent)
        {
            // 从LayerMap中根据layerPath找到ID
            if (LayerMap.TryGetValue(path, out Guid id)) return id;

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

            m_file.AllLayers.Add(layer);

            return id;
        }

        public int GetLayer(string layerPath, Color color)
        {
            // 先根据图层路径执行子图层解决方案
            ChildLayerSolution(layerPath);

            // 从LayerMap中根据layerPath找到ID
            if (!LayerMap.TryGetValue(layerPath, out Guid id))
            {
                // 不存在，需要分情况创建
                if (layerPath.Contains(Layer.PathSeparator))
                {
                    // 这种情况需要先找到父图层，再创建子图层
                    string tmp = layerPath.Replace(Layer.PathSeparator, "-");
                    string[] arr = tmp.Split('-');

                    if (!LayerMap.TryGetValue(arr[0], out Guid parentID))
                    {
                        return 0;// 这个情况应该不会出现
                    }

                    id = CreateChildLayer(layerPath, color, parentID);
                }
                else id = CreateLayer(layerPath, color);
            }

            Layer layer = m_file.AllLayers.FindId(id);

            if (layer == null) return 0;

            return layer.Index;
        }

        public void Write()
        {
            foreach (KeyValuePair<ObjectAttributes, GeometryBase> item in ObjectMap)
            {
                var obj = item.Value;
                var attr = item.Key;
                if (obj == null) return;
                switch (obj.ObjectType)
                {
                    case ObjectType.Brep:
                        m_file.Objects.AddBrep(obj as Brep, attr);
                        break;
                    case ObjectType.Curve:
                        m_file.Objects.AddCurve(obj as Curve, attr);
                        break;
                    case ObjectType.Point:
                        m_file.Objects.AddPoint((obj as Rhino.Geometry.Point).Location, attr);
                        break;
                    case ObjectType.Surface:
                        m_file.Objects.AddSurface(obj as Surface, attr);
                        break;
                    case ObjectType.Mesh:
                        m_file.Objects.AddMesh(obj as Mesh, attr);
                        break;
                    default:
                        break;
                }
            }
            m_file.Write(m_path, 6);
            m_file.Dispose();
        }
    }

}
