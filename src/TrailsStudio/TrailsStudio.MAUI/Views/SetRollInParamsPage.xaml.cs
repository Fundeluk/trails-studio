using System.Diagnostics;
using TrailsStudio.MAUI.ViewModels;

namespace TrailsStudio.MAUI;

public partial class SetRollInParamsPage : ContentPage
{

	public SetRollInParamsPage()
	{
		InitializeComponent();
        BindingContext = new RollInParamsViewModel(Navigation);        
    }   
}