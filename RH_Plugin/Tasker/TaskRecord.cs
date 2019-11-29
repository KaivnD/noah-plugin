using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Tasker
{
    public class TaskRecord
    {
        public Guid ID { get; set; }
        public DateTime date { get; set; }
        public string table { get; set; }
    }
}
