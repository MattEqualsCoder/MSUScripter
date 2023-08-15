namespace MSUScripter.Tools;

public class BasicEventArgs
{
    public string? Data { get; set; }

    public BasicEventArgs(string? data)
    {
        Data = data;
    }
}