using System.Text;

namespace FastCdcFs.Net.Shell;

internal class ConsoleGrid(int columns)
{
    private readonly List<string[]> rows = [];

    public void Add(params string[] row)
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
                sb.Append(row[i].PadRight(columnWidths[i] + 2));
            }

            sb.AppendLine();
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
                if (row[i].Length > widths[i])
                    widths[i] = row[i].Length;
            }
        }
        return widths;
    }
}
