using Assets.Scripts.Utilities;
using System.Collections;
using UnityEngine;
using Assets.Scripts.Builders;
using System;

namespace Assets.Scripts.Managers
{
    public class BuildManager : Singleton<BuildManager>
    {
        [SerializeField]
        private GameObject takeoffBuilderPrefab;

        [SerializeField]
        private GameObject landingBuilderPrefab;

        /// <summary>
        /// If a slope change is built, but there is nothing built farther from its Start than its length, it is active
        /// </summary>
        public SlopeChange activeSlopeChange = null;

        /// <summary>
        /// If a line element is being built, its builder can be accessed here.
        /// </summary>
        public IObstacleBuilder activeBuilder = null;


        public TakeoffPositionHighlighter StartTakeoffBuild()
        {
            if (activeBuilder != null)
            {
                throw new System.Exception("Cannot Start a new build while another build is in progress.");                
            }

            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            TakeoffBuilder takeoffBuilder = Instantiate(takeoffBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection() * 3f, Quaternion.LookRotation(lastLineElement.GetRideDirection())).GetComponent<TakeoffBuilder>();
            takeoffBuilder.Initialize();
            activeBuilder = takeoffBuilder;

            return takeoffBuilder.GetComponent<TakeoffPositionHighlighter>();
        }

        public LandingPositionHighlighter StartLandingBuild()
        {
            if (activeBuilder != null)
            {
                throw new System.Exception("Cannot Start a new build while another build is in progress.");
            }

            ILineElement lastLineElement = Line.Instance.GetLastLineElement();

            LandingBuilder landingBuilder = Instantiate(landingBuilderPrefab, lastLineElement.GetEndPoint() + lastLineElement.GetRideDirection() * 1.5f, Quaternion.LookRotation(lastLineElement.GetRideDirection())).GetComponent<LandingBuilder>();
            landingBuilder.Initialize();
            activeBuilder = landingBuilder;

            return landingBuilder.GetComponent<LandingPositionHighlighter>();
        }
    }
}