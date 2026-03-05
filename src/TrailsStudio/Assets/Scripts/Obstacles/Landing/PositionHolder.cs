using UnityEngine;

namespace Obstacles.Landing
{
    public class PositionHolder : MonoBehaviour
    {
        public LandingPositioner.LandingPositionCarrier TrajectoryPositionInfo { get; private set; }

        public void Init(LandingPositioner.LandingPositionCarrier trajectoryPositionInfo)
        {
            this.TrajectoryPositionInfo = trajectoryPositionInfo;
        }
    }
}