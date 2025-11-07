using System.Globalization;
using System.Linq;

namespace MSUScripter.Tools;

public static class StringExtensions
{
    public static string CleanString(this string str)
    {
        return new string(str.Where(c => c >= 0x1F).ToArray());
    }

    public static int GetUnicodeLength(this string str)
    {
        return new StringInfo(str.CleanString()).LengthInTextElements;
    }

    public static decimal? VersionStringToDecimal(this string str)
    {
        if (!str.Contains('.'))
        {
            return null;
        }

        if (str.StartsWith('v'))
        {
            str = str[1..];
        }

        var parts = str.Split('.');
        if (parts.Length <= 2)
        {
            return decimal.Parse(str);
        }
        else if (parts.Length == 3)
        {
            return decimal.Parse(parts[0]) * 1000 + decimal.Parse(parts[1]) + decimal.Parse(parts[2]) / 1000;
        }
        else if (parts.Length == 4)
        {
            return decimal.Parse(parts[0]) * 1000000 + decimal.Parse(parts[1]) * 1000 + decimal.Parse(parts[2]) + decimal.Parse(parts[3]) / 1000;
        }

        return null;
    }
}