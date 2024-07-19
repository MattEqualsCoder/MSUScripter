using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaControls.Controls;
using AvaloniaControls.Extensions;
using MSUScripter.Models;
using MSUScripter.Services.ControlServices;
using MSUScripter.ViewModels;

namespace MSUScripter.Views;

public partial class EditProjectPanel : UserControl
{
    private readonly EditProjectPanelService? _service;
    
    public static readonly StyledProperty<MainWindowViewModel?> ParentDataContextProperty = AvaloniaProperty.Register<EditProjectPanel, MainWindowViewModel?>(
        nameof(ParentDataContext));

    public MainWindowViewModel? ParentDataContext
    {
        get => GetValue(ParentDataContextProperty);
        set => SetValue(ParentDataContextProperty, value);
    }

    public EditProjectPanelViewModel Model { get; private set;  } = new();
    
    public EditProjectPanel()
    {
        InitializeComponent();
        
        if (Design.IsDesignMode)
        {
            DataContext = Model = (EditProjectPanelViewModel)new EditProjectPanelViewModel().DesignerExample();
        }
        else
        {
            _service = this.GetControlService<EditProjectPanelService>()!;
        }
        
        ParentDataContextProperty.Changed.Subscribe(x =>
        {
            if (x.Sender != this || x.NewValue.Value == null || _service == null)
            {
                return;
            }

            x.NewValue.Value.CurrentMsuProjectChanged += (sender, args) =>
            {
                if (x.NewValue.Value.CurrentMsuProject != null)
                {
                    DataContext = Model = _service.InitializeModel(x.NewValue.Value.CurrentMsuProject);
                }
            };
            
        });
        
        try
        {
            HotKeyManager.SetHotKey(this.Find<MenuItem>(nameof(SaveMenuItem))!, new KeyGesture(Key.S, KeyModifiers.Control));
        }
        catch
        {
            // Do nothing
        }
    }

    public event EventHandler? OnCloseProject;

    public bool HasPendingChanges => _service?.HasPendingChanges() == true;

    private void PrevButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.IncrementPage(-1);
    }

    private void NextButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.IncrementPage(1);
    }

    private void TrackOverviewMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SetPage(1);
    }

    private void MsuDetailsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SetPage(0);
    }

    private async void SettingsMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow();
        await settingsWindow.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }

    private void SaveMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.SaveProject();
    }

    private async void NewMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.HasPendingChanges() == true)
        {
            await DisplayPendingChangesWindow();
        }

        _service?.Disable();
        OnCloseProject?.Invoke(this, EventArgs.Empty);
    }

    private async void AddSongButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.MsuProjectViewModel == null) return;
        var window = new AddSongWindow(_service.MsuProjectViewModel, null);
        await window.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }

    private async void AnalysisButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.MsuProjectViewModel == null) return;
        var window = new AudioAnalysisWindow(_service.MsuProjectViewModel);
        await window.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }

    private async void ExportButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.MsuProjectViewModel == null) return;

        var initError = _service.SetupForMsuGenerationWindow();

        if (!string.IsNullOrEmpty(initError))
        {
            await MessageWindow.ShowErrorDialog(initError, "MSU Generation Error", ParentWindow);
            return;
        }
        
        var project = _service.MsuProjectViewModel;
        var window = new MsuPcmGenerationWindow(project, project.BasicInfo.WriteYamlFile);
        await window.ShowDialog(TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!);
    }

    private async void ExportButtonYaml_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = _service?.ExportYaml();
        if (!string.IsNullOrEmpty(result))
        {
            await MessageWindow.ShowErrorDialog(result, "YAML Generation Error", ParentWindow);
        }
    }

    private async void ExportButtonValidateYaml_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = _service?.ValidateProject();
        if (!string.IsNullOrEmpty(result))
        {
            await MessageWindow.ShowErrorDialog(result, "Validation Failed", ParentWindow);
        }
        else
        {
            await MessageWindow.ShowInfoDialog("Generated MSU and YAML file matches the project", "Validation Successful");
        }
    }

    private void ExportButtonTrackList_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.WriteTrackList();
    }

    private void ExportButtonJson_OnClick(object? sender, RoutedEventArgs e)
    {
        _service?.WriteTrackJson();
    }

    private async void ExportButtonSwapper_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = _service?.WriteSwapperBatchFiles();
        if (!string.IsNullOrEmpty(result))
        {
            await MessageWindow.ShowErrorDialog(result, "Script Generation Failed", ParentWindow);
        }
    }

    private async void ExportButtonSmz3_OnClick(object? sender, RoutedEventArgs e)
    {
        var result = _service?.CreateSmz3SplitBatchFile();
        if (!string.IsNullOrEmpty(result))
        {
            await MessageWindow.ShowErrorDialog(result, "Script Generation Failed", ParentWindow);
        }
    }

    private void ExportButtonMsu_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.MsuProjectViewModel == null) return;
        _service.WriteTrackJson();
        var window = new MsuPcmGenerationWindow(_service.MsuProjectViewModel, false);
        window.Show(ParentWindow);
    }

    private async void OpenFolderMenuItem_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.OpenFolder() != true)
        {
            await MessageWindow.ShowErrorDialog("Could not open MSU folder. Make sure the directory exists.", "Could Not Open", ParentWindow);
        }
    }

    private void ExportButtonVideo_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.MsuProjectViewModel == null) return;
        var window = new VideoCreatorWindow(_service.MsuProjectViewModel);
        window.ShowDialog(ParentWindow);
    }

    private void ExportButtonPackage_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_service?.MsuProjectViewModel == null || _service?.ArePcmFilesUpToDate() != true) return;
        var packageWindow = new PackageMsuWindow(_service.MsuProjectViewModel);
        _ = packageWindow.ShowDialog(ParentWindow);
    }

    public async Task DisplayPendingChangesWindow()
    {
        if (await MessageWindow.ShowYesNoDialog("You currently have unsaved changes. Do you want to save your changes?",
                "Save Changes?", ParentWindow))
        {
            _service?.SaveProject();
        }
    }

    private Window ParentWindow => TopLevel.GetTopLevel(this) as Window ?? App.MainWindow!;

    private void PopupFlyoutBase_OnOpening(object? sender, EventArgs e)
    {
        _service?.UpdateExportMenuOptions();
    }

    private void TrackOverviewPanel_OnOnSelectedTrack(object? sender, TrackEventArgs e)
    {
        _service?.SetToTrackPage(e.TrackNumber);
    }
}