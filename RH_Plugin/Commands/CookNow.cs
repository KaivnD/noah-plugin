using System;
using System.Collections.Generic;
using System.Drawing;
using Grasshopper;
using Grasshopper.Kernel;
using Noah.Tasker;
using Noah.Utils;
using Rhino;
using Rhino.Commands;
using Rhino.DocObjects;
using Rhino.Geometry;

namespace Noah.Commands
{
    public class CookNow : Command
    {
        static CookNow _instance;
        public CookNow()
        {
            _instance = this;
        }

        ///<summary>The only instance of the BakeCurrent command.</summary>
        public static CookNow Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "CookNow"; }
        }

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            var canvas = Instances.ActiveCanvas;

            if (canvas == null)
            {
                RhinoApp.WriteLine("Canvas can't be null");
                return Result.Failure;
            }

            var ghDoc = canvas.Document;

            if (ghDoc == null)
            {
                RhinoApp.WriteLine("ghDoc can't be null");
                return Result.Failure;
            }

            DocObjectBaker.BakeCurrent(doc);
            return Result.Success;
        }
    }
}