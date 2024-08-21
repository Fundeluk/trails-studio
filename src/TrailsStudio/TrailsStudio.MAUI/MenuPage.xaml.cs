namespace TrailsStudio.MAUI;

using Microsoft.Maui.Controls;

public partial class MenuPage : ContentPage
{
	public MenuPage()
	{
		InitializeComponent();
	}

	private void OnNewSpotClicked(object sender, EventArgs e)
    {
        Navigation.PushAsync(new SetRollInParamsPage());
    }

	private void OnLoadSpotClicked(object sender, EventArgs e)
    {
        return;
        //Navigation.PushAsync(new LoadSpotPage());
    }

    private void OnSettingsClicked(object sender, EventArgs e)
    {
        return;
        //Navigation.PushAsync(new SettingsPage());
    }

    private void OnExitClicked(object sender, EventArgs e)
    {
        Application.Current.Quit();
    }

}