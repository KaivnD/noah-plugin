using Eto.Drawing;
using Eto.Forms;
using Noah.Tasker;
using Rhino;
using Rhino.UI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.UI
{
    /// <summary>
    /// Required class GUID, used as the panel Id
    /// </summary>
    [System.Runtime.InteropServices.Guid("BA4C57D3-6479-48F6-A693-433B1F721B55")]
    public class HistoryPanel : Panel, IPanel
    {
        public static Guid PanelId => typeof(HistoryPanel).GUID;

        private readonly StackLayout StackLayout;

        /// <summary>
        /// 用于记录所有运行历史
        /// </summary>
        /// <param name="documentSerialNumber"></param>
        public HistoryPanel(uint documentSerialNumber)
        {
            m_document_sn = documentSerialNumber;

            StackLayout = new StackLayout()
            {
                Padding = 10,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };

            Content = new Scrollable()
            {
                Content = StackLayout
            };
        }

        /// <summary>
        /// 一次性设置多个历史记录，用于刚初始化Rhino的时候，把每个工作流的历史记录加载进来
        /// </summary>
        /// <param name="name"></param>
        /// <param name="historyList"></param>
        /// <param name="id"></param>
        internal void SetHistory(string name, List<TaskRecord> historyList)
        {
            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                historyList.ForEach(record => AddHistory(name, record));
            }));
        }

        private TaskHistory GetHistoryByID(Guid id)
        {
            TaskHistory historyGroup = null;

            foreach (var item in StackLayout.Items)
            {
                if (!(item.Control is TaskHistory his)) continue;
                if (his.TaskID != id) continue;
                historyGroup = his;
            }

            return historyGroup;
        }


        internal void AddHistory(string name, TaskRecord record)
        {

            RhinoApp.InvokeOnUiThread(new Action(() =>
            {
                TaskHistory historyGroup = GetHistoryByID(record.ID);

                if (historyGroup == null)
                {
                    var his = new TaskHistory(name, record.ID);
                    his.AddRow(new TaskRow(record.date.ToString("[MM/dd HH:mm:ss]"), record.table));

                    StackLayout.Items.Add(new StackLayoutItem(his));
                }
                else historyGroup.AddRow(new TaskRow(record.date.ToString("[MM/dd HH:mm:ss]"), record.table));
            }));
        }

        private readonly uint m_document_sn;


        #region IPanel methods
        public void PanelShown(uint documentSerialNumber, ShowPanelReason reason)
        {
        }

        public void PanelHidden(uint documentSerialNumber, ShowPanelReason reason)
        {
        }

        public void PanelClosing(uint documentSerialNumber, bool onCloseDocument)
        {
        }
        #endregion IPanel methods
    }
}
