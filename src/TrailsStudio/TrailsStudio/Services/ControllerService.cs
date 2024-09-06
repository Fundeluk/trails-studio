using System;
using System.Collections.Generic;
using System.Text;
using Evergine.Components.Graphics3D;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework;
using System.Runtime.InteropServices;
using Evergine.Framework.Managers;

namespace TrailsStudio.Services
{
    public class ControllerService : Service
    {
        private int rollInHeight;
        private int rollInAngle;
        //private MaterialComponent planeMaterialComponent;
        //private Material grassMaterial;

        protected override void Start()
        {
            base.Start();
            
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            
            var grassMaterial = assetsService.Load<Material>(EvergineContent.Materials.grassMaterial);

            var floor = new Entity("Floor");
            floor.Tag = "Floor";

            floor.AddComponent(new Transform3D())
            .AddComponent(new PlaneMesh())
            .AddComponent(new MaterialComponent() { Material = grassMaterial})
            .AddComponent(new MeshRenderer());

            var screenContextManager = Application.Current.Container.Resolve<ScreenContextManager>();

            var baseScene = assetsService.Load<MyScene>(EvergineContent.Scenes.MyScene_wescene);

            baseScene.Managers.EntityManager.Add(floor);

            ScreenContext screenContext = new ScreenContext(baseScene);

            screenContextManager.To(screenContext);

        }

        public void RegisterRollInParams(int rollInHeight, int rollInAngle)
        {
            this.rollInHeight = rollInHeight;
            this.rollInAngle = rollInAngle;
        }
    }
}
