using Eto.Forms;
using Newtonsoft.Json;
using System.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using Eto.Drawing;

namespace Noah.UI
{
    public class TableViewer : Form
    {
        private readonly string[][] table;
        public TableViewer(string tableJson)
        {
            ClientSize = new Size(600, 400);
            table = JsonConvert.DeserializeObject<string[][]>(tableJson);
            Title = "数据表";

            string[] headerRow = table[0];

            var collection = new ObservableCollection<string[]>();

            for (int i = 1; i < table.GetLength(0); i++)
            {
                if (table[i].Length != headerRow.Length) continue;

                collection.Add(table[i]);
            }

            var grid = new GridView
            {
                ShowHeader = true,
                GridLines = GridLines.Both,
                DataStore = collection
            };

            for (int i = 0; i < headerRow.Length; i++)
            {
                grid.Columns.Add(new GridColumn
                {
                    HeaderText = table[0][i],
                    DataCell = new TextBoxCell(i),
                    AutoSize = true
                });
            }

            Content = grid;
        }
    }
}
