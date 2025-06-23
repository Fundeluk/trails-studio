using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class PositionHolder : MonoBehaviour
    {
        public LandingPositionTrajectoryInfo trajectoryPositionInfo { get; private set; }

        public void Init(LandingPositionTrajectoryInfo trajectoryPositionInfo)
        {
            this.trajectoryPositionInfo = trajectoryPositionInfo;
        }
    }
}