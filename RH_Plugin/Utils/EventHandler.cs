using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Utils
{
    public delegate void InfoHandler(object sender, string message);
    public delegate void WarningHandler(object sender, string message);
    public delegate void ErrorHandler(object sender, string message);

}
