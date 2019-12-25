﻿using Rhino;
using Rhino.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Forms;
using Command = Rhino.Commands.Command;
using Rhino.FileIO;
using Rhino.Runtime;
using Rhino.DocObjects;
using Rhino.Geometry;
using Rhino.DocObjects.Tables;
using Rhino.UI;
using OpenFileDialog = Eto.Forms.OpenFileDialog;

namespace Noah.Commands
{
    public class AutoFillGuide : Command
    {
        public override string EnglishName => "AutoFillGuide";
        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var selectDwgDialog = new OpenFileDialog
            {
                Filters =
                {
                    new FileFilter("AutoCAD Drawings", new string[] { "dwg" })
                }
            };
            if (selectDwgDialog.ShowDialog(RhinoEtoApp.MainWindow) == DialogResult.Ok)
            {
                var options = new FileReadOptions
                {
                    ImportMode = true
                };
                if (!RhinoDoc.ReadFile(selectDwgDialog.FileName, options)) return Result.Failure;

                ZoomNow.ZoomRhinoDoc();
                AllLayerOn();
            }

            int[] baseLayer = GetMultiLayerDialog("选取建筑外轮廓所在图层");
            Layer building = GetLayerDialog("选取建筑外轮廓所在图层");
            if (building == null || baseLayer == null) return Result.Failure;

            Array.ForEach(baseLayer, i => RhinoApp.WriteLine(i.ToString()));

            return Result.Success;
        }

        private Layer GetLayerDialog(string title = "Select Layer")
        {
            bool dialogstate = false;
            int layerIndex = -1;

            if (Dialogs.ShowSelectLayerDialog(ref layerIndex,
                title, false, false, ref dialogstate))
            {
                return RhinoDoc.ActiveDoc.Layers.FindIndex(layerIndex);
            }
            return null;
        }

        private int[] GetMultiLayerDialog(string title = "Select Layer")
        {
            if (Dialogs.ShowSelectMultipleLayersDialog(new int[] { }, title, false, out int[] layers))
            {
                return layers;
            }

            return null;
        }

        private void OneLayerOn(Layer layer)
        {
            RhinoDoc.ActiveDoc.Layers.SetCurrentLayerIndex(layer.Index, true);
            foreach (var ly in RhinoDoc.ActiveDoc.Layers)
            {                
                if (Equals(layer, ly)) continue;
                ly.IsVisible = false;
            }
        }

        private void AllLayerOn()
        {
            foreach (var ly in RhinoDoc.ActiveDoc.Layers)
            {
                ly.IsVisible = true;
            }
        }
    }
}
