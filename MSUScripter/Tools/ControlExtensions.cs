using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Platform.Storage;

namespace MSUScripter.Tools;

public static class ControlExtensions
{
    public static async Task<string?> GetDocumentsFolderPath(this Control control)
    {
        var topLevel = TopLevel.GetTopLevel(control) ?? App.MainWindow;
        if (topLevel == null)
        {
            return null;
        }

        var location = await topLevel.StorageProvider.TryGetWellKnownFolderAsync(WellKnownFolder.Documents);
        return location?.Path.LocalPath;
    }
}