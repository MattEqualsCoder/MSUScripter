using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Styling;
using AvaloniaControls.Controls;
using AvaloniaControls.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MSURandomizerLibrary.Models;
using MSURandomizerLibrary.Services;
using MSUScripter.Controls;
using MSUScripter.Models;
using MSUScripter.Services;
using MessageWindowResult = MSUScripter.Models.MessageWindowResult;

namespace MSUScripter;

public partial class App : Application
{
    public static MainWindow? MainWindow;
    
    public static string Version
    {
        get
        {
            var version = FileVersionInfo.GetVersionInfo(Assembly.GetEntryAssembly()!.Location); 
            return (version.ProductVersion ?? "").Split("+")[0];
        }
    }
    
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownRequested += DesktopOnShutdownRequested;
            
            Current!.RequestedThemeVariant = SettingsService.Instance.Settings.DarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
            
            var mainWindow = Program.MainHost.Services.GetRequiredService<MainWindow>();
            MessageWindow.GlobalParentWindow = mainWindow;
            desktop.MainWindow = MainWindow = mainWindow;
        }
        
        base.OnFrameworkInitializationCompleted();
    }
    
    private void DesktopOnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
    {
        if (MainWindow == null) return;
        
        var hasPendingChanges = MainWindow.CheckPendingChanges();
        if (!hasPendingChanges) return;
        
        e.Cancel = true;

        var window = new MessageWindow(new MessageWindowRequest
        {
            Message = "You currently have unsaved changes. Do you want to save your changes?",
            Icon = MessageWindowIcon.Question,
            Buttons = MessageWindowButtons.YesNo,
            Title = "Save Pending Changes?"
        });
            
        window.Show();

        window.Closing += (o, args) =>
        {
            var result = window.DialogResult;
            if (result?.PressedAcceptButton == true)
            {
                MainWindow.SaveChanges();
            }
        };
    }

}