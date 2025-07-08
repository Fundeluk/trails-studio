using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
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