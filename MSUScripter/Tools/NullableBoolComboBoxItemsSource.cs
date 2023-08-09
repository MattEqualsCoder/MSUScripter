namespace MSUScripter.Tools;

public static class NullableBoolComboBoxItemsSource
{
    public const string Unspecified = "";
    public const string Yes = "Yes";
    public const string No = "No";

    public static readonly string[] ItemsSource = new[]
    {
        Unspecified,
        Yes,
        No
    };
}