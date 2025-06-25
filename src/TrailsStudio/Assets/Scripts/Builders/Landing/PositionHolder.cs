using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Builders
{
    public class PositionHolder : MonoBehaviour
    {
        public LandingPositionTrajectoryInfo TrajectoryPositionInfo { get; private set; }

        public void Init(LandingPositionTrajectoryInfo trajectoryPositionInfo)
        {
            this.TrajectoryPositionInfo = trajectoryPositionInfo;
        }
    }
}