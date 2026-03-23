using System;
using LineSystem;
using Misc;
using Obstacles.Landing;
using Obstacles.TakeOff;
using UnityEngine;

namespace Obstacles
{
    public class BuildManager : Singleton<BuildManager>
    {
        [SerializeField]
        private GameObject takeoffBuilderPrefab;

        [SerializeField]
        private GameObject landingBuilderPrefab;
        
        /// <summary>
        /// If a line element is being built, its builder can be accessed here.
        /// </summary>
        public IObstacleBuilder ActiveBuilder = null;


        public TakeoffPositioner StartTakeoffBuild()
        {
            if (ActiveBuilder != null)
            {
                throw new Exception("Cannot Start a new build while another build is in progress.");                
            }

            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            TakeoffBuilder takeoffBuilder = Instantiate(takeoffBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection() * 8f, 
                Quaternion.LookRotation(lastLineElement.GetRideDirection())).GetComponent<TakeoffBuilder>();

            takeoffBuilder.Initialize();
            ActiveBuilder = takeoffBuilder;

            return takeoffBuilder.GetComponent<TakeoffPositioner>();
        }

        public LandingPositioner StartLandingBuild()
        {
            if (ActiveBuilder != null)
            {
                throw new Exception("Cannot Start a new build while another build is in progress.");
            }

            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            LandingBuilder landingBuilder = Instantiate(landingBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection() * 1.5f, 
                Quaternion.LookRotation(lastLineElement.GetRideDirection())).GetComponent<LandingBuilder>();

            landingBuilder.Initialize();
            ActiveBuilder = landingBuilder;

            return landingBuilder.GetComponent<LandingPositioner>();
        }
    }
}