using Avalonia.Controls;
using MSUScripter.Models;

namespace MSUScripter.Tools;

public static class WindowExtensions
{
    public static WindowRestoreDetails GetWindowRestoreDetails(this Window window)
    {
        var position = window.Position;
        return new WindowRestoreDetails()
        {
            X = position.X,
            Y = position.Y,
            Width = window.Width,
            Height = window.Height
        };
    }
}