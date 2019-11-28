using Eto.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.UI
{
    public class LoggerRow : DynamicLayout
    {
        public LoggerRow(string msg)
        {
            BeginVertical(); // buttons section
            BeginHorizontal();
            Add(new Label() { Text = "[" + DateTime.Now + "]" });
            Add(new Label() { Text = msg });
            EndHorizontal();
            EndVertical();
        }
    }
}
