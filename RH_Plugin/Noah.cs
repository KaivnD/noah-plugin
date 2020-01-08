using System.IO;
using Noah.UI;
using Rhino.PlugIns;
using Rhino.Runtime;
using System.Reflection;
using System;
using Eto.Forms;
using Noah.Utils;
using System.Threading.Tasks;
using System.Diagnostics;

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

        private AutoUpdater AutoUpdater;

        protected override LoadReturnCode OnLoad(ref string errorMessage)
        {
            string updateFeed = "https://ncfz.oss-cn-shanghai.aliyuncs.com/Noah/Plugin/Rhino/channel/";

            #if DEBUG
                string updateChannel = "dev";
            #else
                string updateChannel = "latest";
            #endif


            if (HostUtils.RunningOnOSX)
            {
                updateFeed += $"{updateChannel}-mac.xml";
                string cairo = Path.Combine(AssemblyDirectory, "runtimes", "win-x64", "native", "cairo.dll");
                if (!File.Exists(cairo))
                {
                    Rhino.RhinoApp.WriteLine(cairo);
                } else AssemblyResolver.AddSearchFile(cairo);
                
            }
            else if (HostUtils.RunningOnWindows)
            {
                updateFeed += $"{updateChannel}-win.xml";
            }
            else updateFeed = null;

            if (updateFeed != null)
            {
                AutoUpdater = new AutoUpdater
                {
                    Feed = updateFeed,
                    CurrentVersion = Assembly.GetName().Version.ToString()
                };

                AutoUpdater.OnUpdateAva += Updater_OnUpdateAva;
                Rhino.RhinoApp.Closing += RhinoApp_Closing;
            }

            Rhino.UI.Panels.RegisterPanel(this, typeof(HistoryPanel), "Noah 时光机", null);
            Rhino.UI.Panels.RegisterPanel(this, typeof(LoggerPanel), "Noah 记录本", null);
            return LoadReturnCode.Success;
        }

        private void Updater_OnUpdateAva(object sender, EventArgs e)
        {
            var args = e as CheckUpdateEventArgs;
            if (args == null) return;

            if (args.Error != null)
            {
                MessageBox.Show(args.Error);
                return;
            }

            if (string.IsNullOrEmpty(args.Version)) return;
            var res = MessageBox.Show($"Noah-In-Rhino 新版本{args.Version} 更新可用是否更新 ?", MessageBoxButtons.YesNo, MessageBoxType.Question);
            if (res == DialogResult.Yes)
            {
                string installer = AutoUpdater.DownloadUpdate();
                if (installer == null)
                {
                    MessageBox.Show("更新失败！");
                    return;
                }
                if (File.Exists(installer))
                {
                    Process.Start(installer);
                    return;
                }
                MessageBox.Show(installer);
            }
        }

        private void RhinoApp_Closing(object sender, EventArgs e)
        {
            if (AutoUpdater == null) return;
            AutoUpdater.CheckForUpdate();
        }
    }
}