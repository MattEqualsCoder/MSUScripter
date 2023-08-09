using System.Threading.Tasks;
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
    
    public MessageWindow(string message, MessageWindowType type = MessageWindowType.Basic, string? title = null)
    {
        _message = message;
        _type = type;
        InitializeComponent();
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        
        Title = title ??
            ( _type == MessageWindowType.Error ? "Error"
            : _type == MessageWindowType.Warning ? "Warning"
            : _type == MessageWindowType.Info ? "Info"
            : "MSU Scripter");

        if (message.Length > 120)
        {
            Width = MaxWidth = 500;
            Height = MaxHeight = 200;
        }
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        
        base.OnLoaded(e);
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
                icon.Kind = _type == MessageWindowType.Error ? MaterialIconKind.CloseCircle
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
        await ShowDialog(App._mainWindow);
        return Result;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OkButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.Ok;
        Close();
    }
    
    private void CancelButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.Cancel;
        Close();
    }
    
    private void YesButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.Yes;
        Close();
    }
    
    private void NoButton_OnClick(object? sender, RoutedEventArgs e)
    {
        Result = MessageWindowResult.No;
        Close();
    }
}