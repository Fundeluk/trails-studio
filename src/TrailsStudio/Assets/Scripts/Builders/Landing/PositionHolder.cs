using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class PositionHolder : MonoBehaviour
    {
        public LandingPositionMatchedToTrajectory TrajectoryPositionInfo { get; private set; }

        public void Init(LandingPositionMatchedToTrajectory trajectoryPositionInfo)
        {
            this.TrajectoryPositionInfo = trajectoryPositionInfo;
        }
    }
}