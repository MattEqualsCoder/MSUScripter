using System.ComponentModel;

namespace MSUScripter.Configs;

public static class TrackListTypeDeprecated
{
    public const string List = "List";
    public const string Table = "Table";
    public const string Disabled = "Disabled";

    public static readonly string[] ItemsSource =
    [
        List,
        Table,
        Disabled
    ];
}

public enum TrackList
{
    [Description("List: album - song (artist)")]
    ListAlbumFirst,
    [Description("List: song by artist (album)")]
    ListSongFirst,
    Table,
    Disabled
}