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
        public readonly string Table;
        public readonly Bitmap Bitmap;
        public readonly Bitmap BigBitmap;
        public readonly Guid TaskID;
        public readonly string TaskName;

        public delegate void TaskRestoreHandler(TaskRow taskRow);
        public event TaskRestoreHandler RestoreEvent;

        public TaskRow(string name, string table, Guid guid, string datetime)
        {
            Table = table;
            var view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
            Padding = new Padding(3);
            Size = new Size(-1, -1);

            TaskName = name;

            TaskID = guid;

            Bitmap = Rhino.UI.EtoExtensions.ToEto(view.CaptureToBitmap(new System.Drawing.Size(120, 90)));
            BigBitmap = Rhino.UI.EtoExtensions.ToEto(view.CaptureToBitmap(new System.Drawing.Size(360, 270)));
            BeginVertical(); // buttons section
            BeginHorizontal();
            Add(new ImageView() { Image = Bitmap });
            Add(new Label() { Text = datetime }) ;
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
            File.WriteAllText(@"D:\task.json", JsonConvert.SerializeObject(new JObject
            {
                ["table"] = Table,
                ["img"] = Bitmap.ToByteArray(ImageFormat.Png)
            }, Formatting.Indented));
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

        private void TaskRow_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            MessageBox.Show("123");
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
