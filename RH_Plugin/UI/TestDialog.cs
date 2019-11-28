using Eto.Drawing;
using Eto.Forms;
using Rhino.UI.Forms;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.UI
{
    class TestDialog : CommandDialog
    {
        public TestDialog()
        {
            Padding = new Padding(10);
            Title = "Hello World";
            Resizable = false;
            Content = new StackLayout()
            {
                Padding = new Padding(0),
                Spacing = 6,
                Items =
        {
          new Label { Text="This is a child dialog..." }
        }
            };
        }
    }
}
