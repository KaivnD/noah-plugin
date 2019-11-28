using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Text;

namespace Noah.UI
{
    public class TaskRow : DynamicLayout
    {
        public TaskRow(string name)
        {
            var view = Rhino.RhinoDoc.ActiveDoc.Views.ActiveView;
            Padding = new Padding(3);
            Size = new Size(-1, -1);
            BeginVertical(); // buttons section
            BeginHorizontal();
            Add(new ImageView() { Image = Rhino.UI.EtoExtensions.ToEto(view.CaptureToBitmap(new System.Drawing.Size(120, 90)))});
            Add(new Label() { Text = name }) ;
            EndHorizontal();
            EndVertical();

            var detailMenu = new ButtonMenuItem
            {
                Text = "Test"
            };

            detailMenu.Click += DetailMenu_Click;

            var menu = new ContextMenu(new MenuItem[] 
            {
                detailMenu
            });

            ContextMenu = menu;

            MouseEnter += TaskRow_MouseEnter;
            MouseLeave += TaskRow_MouseLeave;
            MouseDoubleClick += TaskRow_MouseDoubleClick;
        }

        private void DetailMenu_Click(object sender, EventArgs e)
        {
            // TO DO 展示这条记录的数据表格
            MessageBox.Show("展示这条记录的数据表格");
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
