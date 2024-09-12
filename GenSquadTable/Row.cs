using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GenTable
{
    public interface ReadonlyRow
    {
        public int Count { get; }
        public ExcelFillStyle fillStyle { get; }
        public Color color { get; }
        public object this[int index]
        {
            get;
        }
    }


    public class GenTable
    {
        public static ExcelPackage Generate(List<ReadonlyRow> content,string sheetName) {

            ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
            var package = new ExcelPackage();
            {
                var worksheet = package.Workbook.Worksheets.Add(sheetName);
                var x = 1;
                var y = 1;
                foreach (var row in content)
                {
                    for (int i = 0; i < row.Count; i++)
                    {
                        worksheet.Cells[x, y + i].Value = row[i];
                    }
                    var rowObj = worksheet.Cells[x, 1, x, worksheet.Dimension.End.Column];
                    rowObj.Style.Fill.PatternType = row.fillStyle;
                    rowObj.Style.Fill.BackgroundColor.SetColor(row.color);
                    ++x;
                }
                return package;
            }
        }
    }
}
