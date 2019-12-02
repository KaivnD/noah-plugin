using Eto.Drawing;
using Eto.Forms;
using Noah.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.UI
{
    public class HistoryStoreMemo : Dialog
    {
        public delegate void SubmitHandler(string title, string memo);
        public event SubmitHandler SubmitEvent;

        private TextBox title;
        private RichTextArea memo;
        public HistoryStoreMemo()
        {
            Title = "储存详细信息";
            ClientSize = new Size(300, 200);

            var layout = new DynamicLayout()
            {
                Padding = new Padding(5),
                Spacing = new Size(3, 3)
            };

            title = new TextBox
            {
                PlaceholderText = "标题"
            };

            layout.AddRow(title);

            memo = new RichTextArea
            {
                Size = new Size(-1, 135)
            };

            layout.AddRow(memo);

            var noBtn = new Button { Text = "取消" };
            noBtn.Click += (sd, e) => Close();
            var okBtn = new Button { Text = "确定" };
            okBtn.Click += OkBtn_Click;

            var btnGroup = new DynamicLayout
            {
                Size = new Size(-1, 12),
                Spacing = new Size(3, 3)
            };

            btnGroup.AddRow(null, noBtn, okBtn);

            layout.AddRow(btnGroup);

            Content = layout;
        }

        private void OkBtn_Click(object sender, EventArgs e)
        {
            SubmitEvent(title.Text, memo.Text);
            Close();
        }
    }
}
