using Eto.Forms;
using Rhino.UI;
using Rhino;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Noah.UI
{
    class Log
    {
        public string Text { get; set; }

        public string Ts { get; set; }
    }
    [System.Runtime.InteropServices.Guid("BC670D72-2114-47B2-A42A-55D6002BC4C4")]
    public class LoggerPanel : Panel, IPanel
    {
        public static Guid PanelId => typeof(LoggerPanel).GUID;

        private readonly uint m_document_sn;
        private GridView StackGrid { get; set; }
        private ObservableCollection<Log> Logs { get; set; }

        public LoggerPanel(uint documentSerialNumber)
        {
            m_document_sn = documentSerialNumber;

            Logs = new ObservableCollection<Log>();

            StackGrid = new GridView()
            {
                ShowHeader = false,
                DataStore = Logs
            };

            StackGrid.Columns.Add(new GridColumn()
            {
                HeaderText = "TS",
                Editable = false,
                AutoSize = true,
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Property<Log, string>(r => r.Ts)
                }
            });

            StackGrid.Columns.Add(new GridColumn()
            {
                HeaderText = "Message",
                Editable = false,
                AutoSize = true,
                DataCell = new TextBoxCell
                {
                    Binding = Binding.Property<Log, string>(r => r.Text)
                }
            });

            Content = new Scrollable()
            {
                Content = StackGrid
            };

            Info("Noah Logger initialize complete");
        }

        public void Info(string msg)
        {
            RhinoApp.InvokeOnUiThread(new Action(() => { Update(msg); }));
        }

        private void Update(string msg)
        {
            Logs.Add(new Log { Ts = DateTime.Now.ToString("[MM/dd HH:mm:ss]"), Text = msg });
        }

        public void PanelClosing(uint documentSerialNumber, bool onCloseDocument)
        {
        }

        public void PanelHidden(uint documentSerialNumber, ShowPanelReason reason)
        {
        }

        public void PanelShown(uint documentSerialNumber, ShowPanelReason reason)
        {
        }
    }
}
