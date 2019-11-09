using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using Rhino;
using Rhino.Commands;
using Rhino.Input;
using Rhino.Runtime;

namespace NoahPlugin
{
    public class NoahListener : Command
    {
        static NoahListener _instance;
        public NoahListener()
        {
            _instance = this;
        }

        ///<summary>The only instance of the NoahListener command.</summary>
        public static NoahListener Instance
        {
            get { return _instance; }
        }

        public override string EnglishName
        {
            get { return "NoahListener"; }
        }

        private string NOAH_GENERATOR = string.Empty;
        private string NOAH_PROJECT = string.Empty;
        private JObject ProjectInfo = null;

        protected override Result RunCommand(RhinoDoc doc, RunMode mode)
        {
            string args = "";
            Result getArgs = RhinoGet.GetString("启动参数", true, ref args);
            if (getArgs == Result.Success)
            {
                Dictionary<string, string> argv = new Dictionary<string, string>();
                foreach (string arg in args.Split('&'))
                {
                    if (arg.Length > 0)
                    {
                        string[] argArr = arg.Split('=');
                        if (argArr.Length == 2)
                        {
                            argv[argArr[0]] = argArr[1];
                            SetVal(argArr[0], argArr[1]);
                        }
                    }
                }
                string path = Path.GetDirectoryName(argv["NOAH_PROJECT"]);
                NOAH_GENERATOR = argv["NOAH_GENERATOR"];
                RhinoApp.WriteLine("Project: {0}", argv["NOAH_PROJECT"]);
                if (argv["NOAH_PROJECT"] != string.Empty)
                {
                    if (File.Exists(argv["NOAH_PROJECT"]))
                    {
                        NOAH_PROJECT = argv["NOAH_PROJECT"];
                        updateParams();
                    }
                    FileSystemWatcher watcher = new FileSystemWatcher();
                    watcher.Path = Path.GetDirectoryName(argv["NOAH_PROJECT"]);

                    // Watch for changes in LastAccess and LastWrite times, and
                    // the renaming of files or directories.
                    watcher.NotifyFilter = NotifyFilters.LastWrite;
                    // Only watch text files.
                    watcher.Filter = Path.GetFileName(argv["NOAH_PROJECT"]);

                    // Add event handlers.
                    watcher.Changed += new FileSystemEventHandler(OnChanged);

                    // Begin watching.
                    watcher.EnableRaisingEvents = true;
                }
                else RhinoApp.WriteLine("没有参数");
            }
            RhinoApp.WriteLine("The {0} is running right now.", EnglishName);
            return Result.Success;
        }

        private int counter = 0;

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            RhinoApp.WriteLine("{0}", e.FullPath);
            if (counter == 1 && e.ChangeType.Equals(WatcherChangeTypes.Changed))
            {
                if (NormalizePath(e.FullPath) == NormalizePath(NOAH_PROJECT))
                {
                    System.Windows.Forms.Control.CheckForIllegalCrossThreadCalls = false;
                    updateParams();
                    reSolution();
                    --counter;
                }
            }
            else
            {
                ++counter;
            }
        }

        public static string NormalizePath(string path)
        {
            return Path.GetFullPath(new Uri(path).LocalPath)
                       .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                       .ToUpperInvariant();
        }

        private static char[] base26Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ".ToCharArray();

        private JObject findJobjectFromJArray(JArray arr, string guid)
        {
            for (int i = 0; i < arr.Count; i ++)
            {
                if ((string)arr[i]["guid"] == guid)
                {
                    return (JObject)arr[i];
                }
            }
            return null;
        }

        private void updateParams()
        {
            updateProject();
            if (ProjectInfo != null)
            {
                JArray generators = JArray.Parse(ProjectInfo["generators"].ToString());
                var generator = findJobjectFromJArray(generators, NOAH_GENERATOR);
                JArray inputs = (JArray)generator["input"];
                JArray table = (JArray)generator["table"];

                for (int i = 0; i < table.Count; i++)
                {
                    JArray row = (JArray)table[i];
                    for (int j = 0; j < row.Count; j++)
                    {
                        string key = "@" + base26Chars[j] + i;
                        SetVal(key, table[i][j].ToString());
                    }
                }

                for (int i = 0; i < inputs.Count; i++)
                {
                    JArray cn = (JArray)inputs[i]["connection"];
                    if (cn.Count > 0)
                    {
                        JObject cndn = getConnectedNode(JObject.Parse(cn[0].ToString()));
                        string name = inputs[i]["name"].ToString();
                        string value = cndn["value"].ToString();
                        SetVal(name, cndn["value"].ToString());
                    }
                }
            }
        }

        private JObject getConnectedNode(JObject cn)
        {
            JArray generators = JArray.Parse(ProjectInfo["generators"].ToString());
            int genIndex = (int)cn["g"];
            string io = (string)cn["io"];
            int nodeIndex = (int)cn["n"];
            JObject node = JObject.Parse(generators[genIndex][io][nodeIndex].ToString());
            return node;
        }

        private void updateProject()
        {
            PythonScript py = PythonScript.Create();
            py.SetVariable("file", NOAH_PROJECT);
            string script =
              "import scriptcontext as sc\n" +
              "import sys\n" +
              "sys.setdefaultencoding('utf-8')\n" +
              "with open(file, 'r') as f:\n" +
              "\t\tproject = f.read()\n";
            try
            {
                py.ExecuteScript(script);
                ProjectInfo = JObject.Parse((string)py.GetVariable("project"));
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.Message);
            }

        }

        private void reSolution()
        {
            PythonScript script = PythonScript.Create();
            try
            {
                script.ExecuteScript(
                    "import Rhino\n" +
                    "gh = Rhino.RhinoApp.GetPlugInObject('Grasshopper')\n" +
                    "gh.RunSolver(True)"
                    );
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.Message);
            }
        }

        private void SetVal(string key, object val)
        {
            PythonScript script = PythonScript.Create();
            try
            {
                script.SetVariable("V", val);
                script.ExecuteScript(
                    "import scriptcontext as sc\n" +
                    "import sys\n" +
                    "sys.setdefaultencoding('utf-8')\n" +
                    "sc.sticky['" + key + "'] = V");
            }
            catch (Exception ex)
            {
                RhinoApp.WriteLine(ex.Message);
            }
        }
    }
}