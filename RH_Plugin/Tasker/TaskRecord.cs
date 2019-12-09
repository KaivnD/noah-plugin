using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Tasker
{
    public class TaskRecord
    {
        public Guid TaskID { get; set; }
        public Guid HistoryID { get; set; }
        public DateTime date { get; set; }
        public string table { get; set; }
        public string taskDatas { get; set; }
        public string name { get; set; }

        public string title { get; set; }
        public string memo { get; set; }
        public byte[] thumbnail { get; set; }
        public byte[] bigImage { get; set; }
    }
}
