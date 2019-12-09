using Eto.Drawing;
using Eto.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Noah.Tasker;
using Noah.Utils;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Noah.UI
{
    public class TaskRow : DynamicLayout
    {
        public string Table;
        public Bitmap thumbnail;
        public Bitmap BigBitmap;
        public Guid TaskID;
        public string TaskName;
        public Guid HistoryID;
        public List<TaskData> TaskDatas { set; get; }
        public string title;
        public string memo;

        public delegate void TaskRowHandler(TaskRow taskRow);
        public event TaskRowHandler RestoreEvent;
        public event TaskRowHandler StoreEvent;

        public TaskRow(int index, TaskRecord record)
        {

            Table = record.table;
            TaskID = record.TaskID;
            TaskName = string.Format("{0}({1})", record.name, record.TaskID.ToString().Split('-')[0]);
            HistoryID = record.HistoryID;

            TaskDatas = JsonConvert.DeserializeObject<List<TaskData>>(record.taskDatas);

            if (record.title != null)
            {
                title = record.title;
            }

            if (record.memo != null)
            {
                memo = record.memo;
            }

            if (record.thumbnail == null || record.bigImage == null)
            {
                var view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
                thumbnail = Rhino.UI.EtoExtensions.ToEto(view.CaptureToBitmap(new System.Drawing.Size(120, 90)));
                BigBitmap = Rhino.UI.EtoExtensions.ToEto(view.CaptureToBitmap(new System.Drawing.Size(360, 270)));
            } else
            {
                thumbnail = new Bitmap(record.thumbnail);
                BigBitmap = new Bitmap(record.bigImage);
            }

            Padding = new Padding(3);
            Size = new Size(-1, -1);

            BeginVertical(); // buttons section
            BeginHorizontal();
            Add(new ImageView() { Image = thumbnail });
            var layout = new DynamicLayout
            {
                Padding = new Padding(5),
            };

            if (title != null) layout.AddRow(null, new Label { Text = title });
            else layout.AddRow(null, new Label { Text = "#" + index });

            Add(layout);
            EndHorizontal();
            EndVertical();

            var restoreBtn = new ButtonMenuItem
            {
                Text = "恢复"
            };

            restoreBtn.Click += (sd, e) => RestoreEvent(this);

            var detailMenu = new ButtonMenuItem
            {
                Text = "详细"
            };

            detailMenu.Click += DetailMenu_Click;

            var storeBtn = new ButtonMenuItem
            {
                Text = "储存"
            };

            storeBtn.Click += StoreBtn_Click;

            var menu = new ContextMenu(new MenuItem[] 
            {
                restoreBtn,
                detailMenu,
                storeBtn
            });

            ContextMenu = menu;

            MouseEnter += TaskRow_MouseEnter;
            MouseLeave += TaskRow_MouseLeave;
            MouseDoubleClick += (sd, e) => RestoreEvent(this);
        }

        private void StoreBtn_Click(object sender, EventArgs e)
        {
            var dialog = new HistoryStoreMemo();
            dialog.SubmitEvent += (_title, _memo) => 
            {
                title = _title;
                memo = _memo;

                StoreEvent(this);
            };

            Rhino.RhinoApp.InvokeOnUiThread(new Action(() => dialog.ShowModal(Rhino.UI.RhinoEtoApp.MainWindow)));
        }

        private void DetailMenu_Click(object sender, EventArgs e)
        {
            try
            {
                Rhino.RhinoApp.InvokeOnUiThread(new Action(() => 
                {
                    var table = new HistoryDetialWindow(TaskName, Table, BigBitmap);
                    table.ShowModal(Rhino.UI.RhinoEtoApp.MainWindow);
                }));
            }
            catch
            {

            }

        }

        private void TaskRow_MouseLeave(object sender, MouseEventArgs e)
        {
            BackgroundColor = Colors.Transparent;
        }

        private void TaskRow_MouseEnter(object sender, MouseEventArgs e)
        {
            BackgroundColor = Color.FromArgb(0, 0, 0, 100);
        }
    }
}
