using System.ComponentModel;

namespace MSUScripter.Configs;

public enum DitherType
{
    [Description("")]
    Default,
    
    [Description("All Tracks")]
    All,
    
    [Description("No Tracks")]
    None,
    
    [Description("Per Track (Default On)")]
    DefaultOn,
    
    [Description("Per Track (Default Off)")]
    DefaultOff,
}