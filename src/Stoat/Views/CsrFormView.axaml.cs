using Avalonia.Controls;
using Avalonia.Interactivity;
using Stoat.Core.ViewModels;

namespace Stoat.Views;

public partial class CsrFormView : UserControl
{
    public CsrFormView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        // Wire up RadioButton events
        var newKeyRadio = this.FindControl<RadioButton>("NewKeyRadio");
        var existingKeyRadio = this.FindControl<RadioButton>("ExistingKeyRadio");

        if (newKeyRadio != null)
        {
            newKeyRadio.IsCheckedChanged += (s, args) =>
            {
                if (newKeyRadio.IsChecked == true && DataContext is CsrFormViewModel vm)
                {
                    vm.UseExistingKey = false;
                }
            };
        }

        if (existingKeyRadio != null)
        {
            existingKeyRadio.IsCheckedChanged += (s, args) =>
            {
                if (existingKeyRadio.IsChecked == true && DataContext is CsrFormViewModel vm)
                {
                    vm.UseExistingKey = true;
                }
            };
        }
    }
}
