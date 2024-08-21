namespace TrailsStudio.MAUI;

struct RollInParams
{
    public double RollInHeight { get; set; }
    public double RollInAngle { get; set; }
}

public partial class SetRollInParamsPage : ContentPage
{
    RollInParams rollInParams;

	public SetRollInParamsPage()
	{
		InitializeComponent();
        rollInParams = new RollInParams();
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private void OnSetClicked(object sender, EventArgs e)
    {

        return;
        //Navigation.PushAsync(new RollInPage());
    }
}