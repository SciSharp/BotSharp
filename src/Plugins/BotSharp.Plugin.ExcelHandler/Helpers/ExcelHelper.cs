using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System.Globalization;

namespace BotSharp.Plugin.ExcelHandler.Helpers;

internal static class ExcelHelper
{
    internal static IWorkbook ConvertCsvToWorkbook(byte[] bytes)
    {
        IWorkbook workbook = new XSSFWorkbook();
        ISheet sheet = workbook.CreateSheet("Sheet1");

        using var memoryStream = new MemoryStream(bytes);
        using var reader = new StreamReader(memoryStream);

        int rowIndex = 0;
        string? line;

        while ((line = reader.ReadLine()) != null)
        {
            IRow row = sheet.CreateRow(rowIndex);
            var values = ParseCsvLine(line);

            for (int colIndex = 0; colIndex < values.Count; colIndex++)
            {
                ICell cell = row.CreateCell(colIndex);
                var value = values[colIndex];

                if (rowIndex > 0 && double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double numericValue))
                {
                    cell.SetCellValue(numericValue);
                }
                else
                {
                    cell.SetCellValue(value);
                }
            }

            rowIndex++;
        }

        return workbook;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var currentValue = new StringBuilder();
        bool inQuotes = false;

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];

            if (c == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    currentValue.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }
            }
            else if (c == ',' && !inQuotes)
            {
                values.Add(currentValue.ToString());
                currentValue.Clear();
            }
            else
            {
                currentValue.Append(c);
            }
        }

        values.Add(currentValue.ToString());
        return values;
    }
}
