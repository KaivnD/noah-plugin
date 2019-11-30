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

            // TODO FIX 折叠事件冒泡问题
            // MouseDown += TaskHistory_MouseDown;
        }

        private void TaskHistory_MouseDown(object sender, MouseEventArgs e)
        {
            if (!folded) Content = null;
            else Content = TaskRows;

            folded = !folded;
        }

        public void AddRow (TaskRow task)
        {
            TaskRows.Items.Add(task);
            Content = TaskRows;
        }
    }
}
