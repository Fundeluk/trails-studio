using System.Diagnostics;

namespace TrailsStudio.MAUI;

public partial class SetRollInParamsPage : ContentPage
{

	public SetRollInParamsPage()
	{
		InitializeComponent();
        BindingContext = new RollInParamsViewModel();        
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

   
}