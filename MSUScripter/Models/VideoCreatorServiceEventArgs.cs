using System;

namespace MSUScripter.Models;

public class VideoCreatorServiceEventArgs: EventArgs
{
    public bool Successful { get; set; }
    
    public string? Message { get; set; }

    public VideoCreatorServiceEventArgs(bool successful, string? message)
    {
        Successful = successful;
        Message = message;
    }
}