using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace MSUScripter.UI.Tools;

public static class DependencyObjectExtensions
{
    public static void UpdateControlBindings(this DependencyObject obj)
    {
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
        {
            var child = VisualTreeHelper.GetChild(obj, i);
            if (child is Control control && child is TextBox or CheckBox or ComboBox)
            {
                if (child is TextBox textBox)
                {
                    textBox.GetBindingExpression(TextBox.TextProperty)?.UpdateSource();
                }
                else if (child is ComboBox comboBox)
                {
                    comboBox.GetBindingExpression(ItemsControl.ItemsSourceProperty)?.UpdateSource();
                }
                else if (child is CheckBox checkBox)
                {
                    checkBox.GetBindingExpression(ToggleButton.IsCheckedProperty)?.UpdateSource();
                }
            }

            child.UpdateControlBindings();
        }
    }
}