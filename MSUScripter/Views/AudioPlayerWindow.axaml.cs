using System.IO;
using AvaloniaControls.Controls;
using MSUScripter.Models;

namespace MSUScripter.Views;

public partial class AudioPlayerWindow : RestorableWindow
{
    public AudioPlayerWindow()
    {
        InitializeComponent();
    }

    protected override string RestoreFilePath => Path.Combine(Directories.BaseFolder, "Windows", "audio-player-window.json");
    protected override int DefaultWidth => 800;
    protected override int DefaultHeight => 40;
}