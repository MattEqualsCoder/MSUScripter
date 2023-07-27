using System.Windows.Controls;
using MSUScripter.Services;

namespace MSUScripter.UI;

public partial class AudioControl : UserControl
{
    public AudioControl(AudioService audioService)
    {
        InitializeComponent();
    }
}