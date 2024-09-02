using System.Diagnostics;

namespace TrailsStudio.MAUI;

struct RollInParams
{
    public int RollInHeight;
    public int RollInAngle;
}

public partial class SetRollInParamsPage : ContentPage
{
    RollInParams rollInParams;

	public SetRollInParamsPage()
	{
		InitializeComponent();
        BindingContext = new RollInParamsViewModel();
        //rollInParams = new RollInParams();
        
    }

    private void OnBackClicked(object sender, EventArgs e)
    {
        Navigation.PopAsync();
    }

    private void OnSetClicked(object sender, EventArgs e)
    {
        //rollInParams.RollInHeight = int.Parse(RollInHeightEntry.Text);
        //rollInParams.RollInAngle = int.Parse(RollInSlopeEntry.Text);
        Debug.WriteLine($"RollInHeight: {rollInParams.RollInHeight}, RollInAngle: {rollInParams.RollInAngle}");
        return;
        //Navigation.PushAsync(new RollInPage());
    }
}