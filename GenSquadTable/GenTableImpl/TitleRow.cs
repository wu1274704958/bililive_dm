using GenTable;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenTableImpl
{
    public class TitleRow<T> : ReadonlyRow
    {
        private List<T> Items;
        public object this[int index] => Items[index];

        public int Count => Items.Count;

        public ExcelFillStyle fillStyle => ExcelFillStyle.Solid;

        public Color color => Color.LightSlateGray;

        public TitleRow(List<T> items)
        {
            Items = items;
        }
    }
}
