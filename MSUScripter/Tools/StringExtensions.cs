using System.Globalization;
using System.Linq;

namespace MSUScripter.Tools;

public static class StringExtensions
{
    public static string CleanString(this string str)
    {
        return new string(str.Where(c => (int)c >= 0x1F).ToArray());
    }

    public static int GetUnicodeLength(this string str)
    {
        return new StringInfo(str.CleanString()).LengthInTextElements;
    }
}