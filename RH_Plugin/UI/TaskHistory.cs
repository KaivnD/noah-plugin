using Eto.Drawing;
using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Text;

namespace Noah.UI
{
    public class TaskHistory : GroupBox
    {
        public StackLayout TaskRows;

        public Guid TaskID;

        private bool folded;

        private readonly ButtonMenuItem foldButton;

        public TaskHistory(string name, Guid id)
        {
            TaskID = id;
            TaskRows = new StackLayout()
            {
                Padding = 3,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            Text = name + "("+ id.ToString().Split('-')[0] + ")";
            Size = new Size(-1, -1);

            // TODO 清空问题
            //var clearButton = new ButtonMenuItem
            //{
            //    Text = "清空"
            //};

            //clearButton.Click += ClearButton_Click;

            foldButton = new ButtonMenuItem
            {
                Text = "折叠"
            };

            foldButton.Click += FoldButton_Click;

            var menu = new ContextMenu(new MenuItem[]
            {
                foldButton
            });

            ContextMenu = menu;
        }

        private void FoldButton_Click(object sender, EventArgs e)
        {
            if (!folded)
            {
                Content = null;
                foldButton.Text = "展开";
            }
            else
            {
                Content = TaskRows;
                foldButton.Text = "折叠";
            }

            folded = !folded;
        }

        private void ClearButton_Click(object sender, EventArgs e)
        {
            TaskRows.Items.Clear();
        }

        public void AddRow (TaskRow task)
        {
            TaskRows.Items.Add(task);
            Content = TaskRows;
        }
    }
}
