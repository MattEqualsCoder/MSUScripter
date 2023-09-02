namespace MSUScripter.Configs;

public static class TrackListType
{
    public const string List = "List";
    public const string Table = "Table";
    public const string Disabled = "Disabled";

    public static readonly string[] ItemsSource = new[]
    {
        List,
        Table,
        Disabled
    };
}