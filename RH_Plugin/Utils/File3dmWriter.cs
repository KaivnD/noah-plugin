using Grasshopper.Kernel;
using Rhino.DocObjects;
using Rhino.FileIO;
using Rhino.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Utils
{
    public class File3dmWriter
    {
        public string m_path { set; get; }
        public File3dm m_file { set; get; }
        public File3dmLayerTable m_layers { set; get; }
        public File3dmWriter(string path)
        {
            File3dm _file = null;
            if (File.Exists(path))
            {
                try
                {
                    _file = File3dm.Read(path);
                    _file.Objects.Clear();
                    _file.AllLayers.Clear();
                }
                catch
                {
                }
            }
            else
            {
                _file = new File3dm();
            }
            m_file = _file;
            m_layers = _file.AllLayers;
            m_path = path;
        }

        public void ChildLayerSolution(string name)
        {
            if (name.Contains("::"))
            {
                string tmp = name.Replace("::", "-");
                string[] nameArr = tmp.Split('-');
                Layer parent = FindLayerByName(nameArr[0]);
                if (parent == null)
                {
                    Layer l = new Layer();
                    l.Name = nameArr[0];
                    l.Color = Color.Black;
                    m_file.AllLayers.Add(l);
                }
            }
        }

        public int CreateLayer(string layerPath, Color color)
        {
            // 查询指定路径图层是否存在
            Layer layer = FindLayerByFullPath(layerPath);
            int layerIndex = -1;
            if (layer == null)
            {
                // Null不存在，需要创建
                if (layerPath.Contains("::"))
                {
                    // 这种情况需要先创建父图层，再创建子图层
                    string tmp = layerPath.Replace("::", "-");
                    string[] nameArr = tmp.Split('-');
                    Layer parent = FindLayerByName(nameArr[0]);
                    if (parent == null)
                    {
                        Layer l = new Layer();
                        l.Name = nameArr[0];
                        l.Color = Color.Black;
                        m_layers.Add(l);
                        parent = l;
                    }
                    Layer child = FindLayerByFullPath(layerPath);
                    if (child == null)
                    {
                        Layer l = new Layer();
                        l.ParentLayerId = parent.Id;
                        l.Name = nameArr[1];
                        l.Color = color;
                        m_layers.Add(l);
                        layerIndex = m_layers.Count - 1;
                    }
                    else layerIndex = child.Index;
                }
                else
                {
                    // 这种情况直接创建图层
                    Layer la = FindLayerByName(layerPath);
                    if (la == null)
                    {
                        Layer l = new Layer();
                        l.Name = layerPath;
                        l.Color = color;
                        layer = l;
                        m_layers.Add(l);
                        layerIndex = m_layers.Count - 1;
                    }
                    else layerIndex = la.Index;
                }
            }
            else layerIndex = layer.Index;
            return layerIndex;
        }
        private Layer FindLayerByName(string name)
        {
            Layer ll = (from layer in m_file.AllLayers
                        where layer.Name == name
                        select layer).FirstOrDefault();
            return ll;
        }
        private Layer FindLayerByFullPath(string path)
        {
            Layer ll = (from layer in m_file.AllLayers
                        where layer.FullPath == path
                        select layer).FirstOrDefault();
            return ll;
        }

        public void Write(List<ObjectLayerInfo> G, List<int> att)
        {
            for (int i = 0; i < G.Count; i++)
            {
                GeometryBase g = GH_Convert.ToGeometryBase(G[i].Geometry);
                ObjectAttributes attr = new ObjectAttributes();
                attr.LayerIndex = att[i];

                if (g != null)
                {
                    switch (g.ObjectType)
                    {
                        case ObjectType.Brep:
                            m_file.Objects.AddBrep(g as Brep, attr);
                            break;
                        case ObjectType.Curve:
                            m_file.Objects.AddCurve(g as Curve, attr);
                            break;
                        case ObjectType.Point:
                            m_file.Objects.AddPoint((g as Rhino.Geometry.Point).Location, attr);
                            break;
                        case ObjectType.Surface:
                            m_file.Objects.AddSurface(g as Surface, attr);
                            break;
                        case ObjectType.Mesh:
                            m_file.Objects.AddMesh(g as Mesh, attr);
                            break;
                        case ObjectType.PointSet:
                            m_file.Objects.AddPointCloud(g as PointCloud, attr); //This is a speculative entry
                            break;
                        default:
                            break;
                    }
                }
            }
            m_file.Write(m_path, 6);
            m_file.Dispose();
        }
    }
}
