using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Material.Icons;
using Material.Icons.Avalonia;

namespace MSUScripter.Controls;

public partial class MessageWindow : Window
{
    private readonly string _message;
    private readonly MessageWindowType _type;
    
    public MessageWindow()
    {
        _message = "Unknown";
    }
    
    public MessageWindow(string message, MessageWindowType type = MessageWindowType.Basic, string? title = null, Window? parent = null)
    {
        _message = message;
        _type = type;
        
        if (_message.Length > 120)
        {
           Width = MaxWidth = 450;
           Height = MaxHeight = 175;
        }
        
        if (parent == null)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
        }
        else
        {
            var pos = new PixelPoint(parent.Position.X + (int)Math.Floor(parent.Width / 2) - 175,
                parent.Position.Y + (int)Math.Floor(parent.Height / 2));
            Position = pos;
            WindowStartupLocation = WindowStartupLocation.Manual;
        }
        
        InitializeComponent();
        
        Title = title ??
            ( _type == MessageWindowType.Error ? "Error"
            : _type == MessageWindowType.Warning ? "Warning"
            : _type == MessageWindowType.Info ? "Info"
            : "MSU Scripter");
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        if (_message.Length > 120)
        {
            Width = MaxWidth = 450;
            Height = MaxHeight = 175;
        }
        
        var textBlock = this.Find<TextBlock>(nameof(MessageTextBlock));
        if (textBlock == null) return;
        textBlock.Text = _message;
        
        if (_type != MessageWindowType.Basic)
        {
            var iconBorder = this.Find<Border>(nameof(IconBorder));
            if (iconBorder != null) iconBorder.IsVisible = true;

            var icon = this.Find<MaterialIcon>(nameof(MessageIcon));
            if (icon != null)
            {
                icon.Kind = _type is MessageWindowType.Error or MessageWindowType.PcmWarning ? MaterialIconKind.CloseCircle
                    : _type == MessageWindowType.Warning ? MaterialIconKind.Alert
                    : _type == MessageWindowType.Info ? MaterialIconKind.Info
                    : MaterialIconKind.HelpCircle;
            }

            if (_type == MessageWindowType.YesNo)
            {
                this.Find<Button>(nameof(OkButton))!.IsVisible = false;
                this.Find<Button>(nameof(YesButton))!.IsVisible = true;
                this.Find<Button>(nameof(NoButton))!.IsVisible = true;
            }

            if (_type == MessageWindowType.PcmWarning)
            {
                this.Find<CheckBox>(nameof(IgnoreCheckBox))!.IsVisible = true;
            }
        }
        else
        {
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
    }

    public MessageWindowResult? Result { get; private set; }

    public async Task<MessageWindowResult?> ShowDialog()
    {
        if (App._mainWindow == null) return null;
        return await ShowDialog(App._mainWindow);
    }
    
    public new async Task<MessageWindowResult?> ShowDialog(Window window)
    {
        await base.ShowDialog(window);
        return Result;
    }

    public event EventHandler? OnButtonClick; 

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (_type == MessageWindowType.PcmWarning && this.Find<CheckBox>(nameof(IgnoreCheckBox))?.IsChecked == true)
        {
            Result = MessageWindowResult.DontShow;
        }
        else
        {
            Result = MessageWindowResult.Ok;    
        }
        OnButtonClick?.Invoke(this, EventArgs.Empty);
        Close();
    }
    
    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.Cancel;
        OnButtonClick?.Invoke(this, EventArgs.Empty);
        Close();
    }
    
    private void YesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.Yes;
        OnButtonClick?.Invoke(this, EventArgs.Empty);
        Close();
    }
    
    private void NoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.No;
        OnButtonClick?.Invoke(this, EventArgs.Empty);
        Close();
    }

    private void DontShowButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.DontShow;
        OnButtonClick?.Invoke(this, EventArgs.Empty);
        Close();
    }
}