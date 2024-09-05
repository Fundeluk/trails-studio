using System;
using System.Collections.Generic;
using System.Text;
using Evergine.Components.Graphics3D;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework;
using System.Runtime.InteropServices;

namespace TrailsStudio.Services
{
    internal class ControllerService : Service
    {
        private MaterialComponent planeMaterialComponent;
        private Material grassMaterial;

        protected override void Start()
        {
            base.Start();
            
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            
            var grassMaterial = assetsService.Load<Material>(EvergineContent.Materials.grassMaterial);

            var floor = new Entity("Floor");
            floor.Tag = "Floor";

            floor.AddComponent(new Transform3D())
            .AddComponent(new PlaneMesh())
            .AddComponent(new MaterialComponent())
            .AddComponent(new MeshRenderer());

            floor.FindComponent<MaterialComponent>().Material = grassMaterial;



            var screenContextManager = Application.Current.Container.Resolve<ScreenContextManager>();

        }
    }
}
