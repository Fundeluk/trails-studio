using Assets.Scripts.Builders;
using Assets.Scripts.Utilities;
using System.Collections;
using UnityEngine;
using Assets.Scripts.Builders.TakeOff;

namespace Assets.Scripts.Managers
{
    public class BuildManager : Singleton<BuildManager>
    {
        [SerializeField]
        private GameObject takeoffBuilderPrefab;

        [SerializeField]
        private GameObject landingBuilderPrefab;

        /// <summary>
        /// If a slope change is built, but there is nothing built farther from its start than its length, it is active
        /// </summary>
        public SlopeChange activeSlopeChange = null;

        /// <summary>
        /// If a line element is being built, its builder can be accessed here.
        /// </summary>
        public IBuilder activeBuilder = null;


        // TODO the takeoff builder prefab should have the mesh builder component disabled by default
        public TakeoffBuilder StartTakeoffBuild()
        {
            if (activeBuilder != null)
            {
                throw new System.Exception("Cannot start a new build while another build is in progress.");                
            }

            TakeoffBuilder takeoffBuilder = Instantiate(takeoffBuilderPrefab).GetComponent<TakeoffBuilder>();
            takeoffBuilder.Initialize();
            activeBuilder = takeoffBuilder;

            return takeoffBuilder;
        }

        // TODO implement StartLandingBuild
    }
}