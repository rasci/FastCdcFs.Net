using System.Text;

namespace FastCdcFs.Net;

internal class ConsoleGrid(int columns)
{
    private readonly List<object?[]> rows = [];

    public void Add(params object?[] row)
    {
        if (row.Length != columns)
            throw new Exception("row length mismatch");

        rows.Add(row);
    }

    public override string ToString()
    {
        var columnWidths = GetColumnWidths();

        var sb = new StringBuilder();

        foreach (var row in rows)
        {
            for (var i = 0; i < columns; i++)
            {
                sb.Append((row[i]?.ToString() ?? "").PadRight(columnWidths[i] + 2));
            }

            if (rows.Last() != row)
            {
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private int[] GetColumnWidths()
    {
        var widths = new int[columns];

        foreach (var row in rows)
        {
            for (var i = 0; i < columns; i++)
            {
                if (row[i] is null)
                    continue;

                var strLen = row[i]!.ToString()!.Length;
                if (strLen > widths[i])
                    widths[i] = strLen;
            }
        }
        return widths;
    }
}
