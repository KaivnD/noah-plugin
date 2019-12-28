using System.IO;
using Noah.UI;
using Rhino.PlugIns;
using Rhino.Runtime;
using System.Reflection;
using System;

namespace Noah
{
    ///<summary>
    /// <para>Every RhinoCommon .rhp assembly must have one and only one PlugIn-derived
    /// class. DO NOT create instances of this class yourself. It is the
    /// responsibility of Rhino to create an instance of this class.</para>
    /// <para>To complete plug-in information, please also see all PlugInDescription
    /// attributes in AssemblyInfo.cs (you might need to click "Project" ->
    /// "Show All Files" to see it in the "Solution Explorer" window).</para>
    ///</summary>
    public class Noah : Rhino.PlugIns.PlugIn

    {
        public Noah()
        {
            Instance = this;
        }

        public static string AssemblyDirectory
        {
            get
            {
                string codeBase = Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uri = new UriBuilder(codeBase);
                string path = Uri.UnescapeDataString(uri.Path);
                return Path.GetDirectoryName(path);
            }
        }

        ///<summary>Gets the only instance of the Noah plug-in.</summary>
        public static Noah Instance
        {
            get; private set;
        }

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            

            if (HostUtils.RunningOnOSX)
            {
                string cairo = Path.Combine(Path.GetDirectoryName(AssemblyDirectory), "runtimes", "win-x64", "native", "cairo.dll");
                AssemblyResolver.AddSearchFile(cairo);
            }

            Rhino.UI.Panels.RegisterPanel(this, typeof(HistoryPanel), "Noah 时光机", null);
            Rhino.UI.Panels.RegisterPanel(this, typeof(LoggerPanel), "Noah 记录本", null);
            return LoadReturnCode.Success;
        }
    }
}