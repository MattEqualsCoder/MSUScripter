using Avalonia;

namespace MSUScripter.Models;

public class WindowRestoreDetails
{
    public int X { get; set; }
    public int Y { get; set; }
    public double Width { get; set; }
    public double Height { get; set; }

    public PixelPoint GetPosition() => new(X, Y);

    public Size GetSize() => new(Width, Height);
}