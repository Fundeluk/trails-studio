using TrailsStudio.MAUI.ViewModels;

namespace TrailsStudio.MAUI;

public partial class StudioPage : ContentPage
{
	private MyApplication evergineApp;
	
    public StudioPage()
	{
		InitializeComponent();

        this.evergineApp = new MyApplication();
		this.evergineView.Application = this.evergineApp;
		this.BindingContext = new StudioViewModel(this.evergineView);
    }
}