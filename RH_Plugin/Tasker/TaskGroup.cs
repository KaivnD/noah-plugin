using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.Tasker
{
    public class TaskGroup
    {
        public List<NoahTask> tasks;

        public TaskGroup()
        {
            tasks = new List<NoahTask>();
        }

        public void Push(NoahTask task)
        {
            if (task == null) return;
            if (tasks.Contains(task)) return;

            tasks.Add(task);
        }
    }
}
