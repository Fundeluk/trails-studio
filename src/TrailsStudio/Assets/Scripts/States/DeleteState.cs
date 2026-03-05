using Managers;

namespace States
{
    public class DeleteState : State
    {
        protected override void OnEnter()
        {
            CameraManager.Instance.SplineCamView();
            StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.deleteUI);
        }
    }
}
