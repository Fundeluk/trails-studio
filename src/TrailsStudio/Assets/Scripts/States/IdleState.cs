using Managers;
using TerrainEditing;

namespace States
{
    /// <summary>
    /// Default state of the application.
    /// Used for default view and looking around the scene.
    /// </summary>
    public class DefaultState : State
    {
        protected override void OnEnter()
        {
            // unfinished slope changes do not occupy heightmap coordinates, so prevent clearing terrains with such slope being active
            if (TerrainManager.Instance.ActiveSlope == null)
            {
                TerrainManager.Instance.ClearUnusedTerrains();
            }
            
            CameraManager.Instance.SplineCamView();
            StudioUIManager.Instance.ToggleObstacleTooltips(true);
            StudioUIManager.Instance.ToggleESCMenu(true);
            StudioUIManager.Instance.ShowUI(StudioUIManager.Instance.sidebarMenuUI);
        }

        protected override void OnExit()
        {
            StudioUIManager.Instance.HideUI();
            StudioUIManager.Instance.ToggleObstacleTooltips(false);
            TerrainManager.Instance.HideSlopeInfo();
            StudioUIManager.Instance.ToggleESCMenu(false);
        }
    }
}
