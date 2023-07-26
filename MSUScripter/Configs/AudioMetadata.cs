namespace MSUScripter.Configs;

public class AudioMetadata
{
    public string? SongName { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Url { get; set; }

    public bool HasData => !string.IsNullOrEmpty(SongName) || !string.IsNullOrEmpty(Artist) ||
                           !string.IsNullOrEmpty(Album) || !string.IsNullOrEmpty(Url);
}