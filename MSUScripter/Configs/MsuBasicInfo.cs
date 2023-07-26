namespace MSUScripter.Configs;

public class MsuBasicInfo
{
    public string MsuType { get; set; } = "";
    public string Game { get; set; } = "";
    public string? PackName { get; set; } = "";
    public string? PackCreator { get; set; }
    public string? PackVersion { get; set; }
    public string? Artist { get; set; }
    public string? Album { get; set; }
    public string? Url { get; set; }
    public double? Normalization { get; set; }
    public bool? Dither { get; set; }
    public bool IsMsuPcmProject { get; set; } = true;
}