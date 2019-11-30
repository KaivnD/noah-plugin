using Eto.Forms;
using Rhino.UI;
using Rhino;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eto.Drawing;
using System.IO;

namespace Noah.UI
{
    public class Log
    {
        public Image LogLevel { get; set; }
        public string Text { get; set; }

        public string Ts { get; set; }
    }
    [System.Runtime.InteropServices.Guid("BC670D72-2114-47B2-A42A-55D6002BC4C4")]
    public class LoggerPanel : Panel, IPanel
    {
        public static Guid PanelId => typeof(LoggerPanel).GUID;

        private GridView StackGrid { get; set; }
        private ObservableCollection<Log> Logs { get; set; }

        public LoggerPanel(uint documentSerialNumber)
        {
            Logs = new ObservableCollection<Log>();

            StackGrid = new GridView()
            {
                ShowHeader = false,
                DataStore = Logs,
                GridLines = GridLines.Vertical
            };

            StackGrid.Columns.Add(new GridColumn()
            {
                HeaderText = "Level",
                Width = 12,
                Editable = false,
                DataCell = new ImageViewCell
                {
                    Binding = Binding.Delegate((Log m) => m.LogLevel)
                }
            }) ;

            StackGrid.Columns.Add(new GridColumn()
            {
                HeaderText = "TS",
                Editable = false,
                AutoSize = true,
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Property<Log, string>(r => r.Ts)
                }
            });

            StackGrid.Columns.Add(new GridColumn()
            {
                HeaderText = "Message",
                Editable = false,
                AutoSize = true,
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Property<Log, string>(r => r.Text)
                }
            });

            Content = new Scrollable()
            {
                Content = StackGrid
            };

            var clearBtn = new ButtonMenuItem
            {
                Text = "CLear"
            };

            clearBtn.Click += ClearBtn_Click;

            ContextMenu = new ContextMenu(new MenuItem[] { clearBtn });

            Info("Noah Logger initialize complete");
        }

        private void ClearBtn_Click(object sender, EventArgs e)
        {
            RhinoApp.InvokeOnUiThread(new Action(() => Logs.Clear()));
        }

        public void Info(string msg)
        {
            RhinoApp.InvokeOnUiThread(new Action(() => Logs.Add(new Log
            {
                Ts = DateTime.Now.ToString("[MM/dd HH:mm:ss]"),
                Text = msg,
                LogLevel = LevelIcon(Colors.Green)
            })));
        }

        public void Debug(string msg)
        {
            RhinoApp.InvokeOnUiThread(new Action(() => Logs.Add(new Log
            {
                Ts = DateTime.Now.ToString("[MM/dd HH:mm:ss]"),
                Text = msg,
                LogLevel = LevelIcon(Colors.Blue)
            })));
        }

        public void Warning(string msg)
        {
            RhinoApp.InvokeOnUiThread(new Action(() => Logs.Add(new Log
            {
                Ts = DateTime.Now.ToString("[MM/dd HH:mm:ss]"),
                Text = msg,
                LogLevel = LevelIcon(Colors.Yellow)
            })));
        }

        public void Error(string msg)
        {
            RhinoApp.InvokeOnUiThread(new Action(() => Logs.Add(new Log
            {
                Ts = DateTime.Now.ToString("[MM/dd HH:mm:ss]"),
                Text = msg,
                LogLevel = LevelIcon(Colors.Red)
            })));
        }

        private Bitmap LevelIcon(Color color)
        {
            Bitmap bitmap = new Bitmap(12, 12, PixelFormat.Format32bppRgba);
            Graphics graphics = new Graphics(bitmap)
            {
                AntiAlias = true
            };
            Brush brush = new SolidBrush(color);

            graphics.FillEllipse(brush, 1, 1, 10, 10);
            graphics.Dispose();
            return bitmap;
        }

        public void PanelClosing(uint documentSerialNumber, bool onCloseDocument)
        {
        }

        public void PanelHidden(uint documentSerialNumber, ShowPanelReason reason)
        {
        }

        public void PanelShown(uint documentSerialNumber, ShowPanelReason reason)
        {
        }
    }
}
