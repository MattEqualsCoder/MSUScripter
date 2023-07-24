using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.WindowsAPICodePack.Dialogs;
using MSUScripter.Configs;
using MSUScripter.ViewModels;

namespace MSUScripter.UI;

public partial class MsuSongInfoPanel : UserControl
{
    private MsuTrackInfoPanel? _parent;
    
    public MsuSongInfoPanel() : this(null, false)
    {
    }
    
    public MsuSongInfoPanel(MsuTrackInfoPanel? parent, bool isAltTrack)
    {
        _parent = parent;
        InitializeComponent();
        DataContext = MsuSongInfo = new MsuSongInfoViewModel();
        if (!isAltTrack)
        {
            OutputPathButton.IsEnabled = false;
        }
    }
    
    public MsuSongInfoViewModel MsuSongInfo { get; set; }

    public void ApplyMsuSongMsuPcmInfo(MsuSongMsuPcmInfo data)
    {
        MsuSongMsuPcmInfoPanel.ApplyMsuSongMsuPcmInfo(data);
    }

    public void SetCanDelete(bool canDelete)
    {
        RemoveButton.IsEnabled = canDelete;
    }


    private void OutputPathButton_OnClick(object sender, RoutedEventArgs e)
    {
        using var dialog = new CommonSaveFileDialog()
        {
            EnsurePathExists = true,
            Title = "Select PCM File",
            DefaultExtension = ".pcm",
            AlwaysAppendDefaultExtension = true,
            Filters = { new CommonFileDialogFilter("PCM Files", "*.pcm") }
        };
        
        if (dialog.ShowDialog() != CommonFileDialogResult.Ok || string.IsNullOrEmpty(dialog.FileName)) return;

        MsuSongInfo.OutputPath = dialog.FileName;
    }

    private void RemoveButton_OnClick(object sender, RoutedEventArgs e)
    {
        if (_parent != null)
        {
            _parent.RemoveSong(this);
        }
    }
}