using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MSUScripter.ViewModels;

namespace MSUScripter.Controls;

public partial class MsuBasicInfoPanel : UserControl
{
    public MsuBasicInfoPanel() : this(new MsuBasicInfoViewModel())
    {
    }
    
    public MsuBasicInfoPanel(MsuBasicInfoViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}