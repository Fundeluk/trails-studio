using System;
using System.Collections.Generic;
using System.Text;
using Evergine.Components.Graphics3D;
using Evergine.Common.Graphics;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Framework;
using System.Runtime.InteropServices;
using Evergine.Framework.Managers;
using Evergine.Framework.Physics3D;
using Evergine.Mathematics;
using Evergine.Components.Cameras;
using TrailsStudio.Components;
using System.Runtime.CompilerServices;

namespace TrailsStudio.Services
{
    public class ControllerService : Service
    {
        const int FLOOR_SIZE = 1000;

        private int rollInHeight;
        private int rollInAngle;

        private Entity floor;
        private Entity camera;

        //private MaterialComponent planeMaterialComponent;
        //private Material grassMaterial;

        protected override void Start()
        {
            base.Start();
            
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            
            var baseScene = assetsService.Load<MyScene>(EvergineContent.Scenes.MyScene_wescene);

            floor = CreateFloor(baseScene, assetsService, FLOOR_SIZE, FLOOR_SIZE);

            baseScene.Managers.EntityManager.Add(floor);

            CreateRollIn(baseScene, assetsService, rollInAngle, rollInHeight);

            camera = CreateCamera(baseScene);

            baseScene.Managers.EntityManager.Add(camera);
        }

        public void RegisterRollInParams(int rollInHeight, int rollInAngle)
        {
            this.rollInHeight = rollInHeight;
            this.rollInAngle = rollInAngle;
        }

        private Entity CreateFloor(Scene scene, AssetsService assetsService, int width, int height) 
        {
            var grassMaterial = assetsService.Load<Material>(EvergineContent.Materials.grassMaterial);

            Entity floor = new Entity()
                .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = grassMaterial })
                .AddComponent(new PlaneMesh() { Width = width, Height = height})
                .AddComponent(new MeshRenderer())
                .AddComponent(new StaticBody3D())
                .AddComponent(new BoxCollider3D());

            return floor;
        }

        private void CreateRollIn(Scene scene, AssetsService assetsService, int angle, int height)
        {
            var legMaterial = assetsService.Load<Material>(EvergineContent.Materials.woodenLogMaterial);
            //var plankMaterial = scene.Managers.AssetSceneManager.Load<Material>(EvergineContent.Materials.woodenPlankMaterial);

            var frontLeftLeg = new Entity()
            .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = legMaterial })
                .AddComponent(new CylinderMesh() { Diameter = 2, Height = height })
                .AddComponent(new MeshRenderer())
                .AddComponent(new StaticBody3D());

            floor.AddChild(frontLeftLeg);

            frontLeftLeg.FindComponent<Transform3D>().SetLocalTransform(new Vector3(0, 15, 0), Quaternion.Identity, Vector3.One);

            var frontRightLeg = new Entity()
                .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = legMaterial })
                .AddComponent(new CylinderMesh() { Diameter = 2, Height = height })
                .AddComponent(new MeshRenderer())
                .AddComponent(new StaticBody3D());

            floor.AddChild(frontRightLeg);

            frontRightLeg.FindComponent<Transform3D>().SetLocalTransform(new Vector3(15, 15, 0), Quaternion.Identity, Vector3.One);

            var rearLeftLeg = new Entity()
                .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = legMaterial })
                .AddComponent(new CylinderMesh() { Diameter = 2, Height = height })
                .AddComponent(new MeshRenderer())
                .AddComponent(new StaticBody3D());

            floor.AddChild(rearLeftLeg);

            rearLeftLeg.FindComponent<Transform3D>().SetLocalTransform(new Vector3(15, 0, 0), Quaternion.Identity, Vector3.One);

            var rearRightLeg = new Entity()
                .AddComponent(new Transform3D())
                .AddComponent(new MaterialComponent() { Material = legMaterial })
                .AddComponent(new CylinderMesh() { Diameter = 2, Height = height })
                .AddComponent(new MeshRenderer())
                .AddComponent(new StaticBody3D());

            floor.AddChild(rearRightLeg);

            rearRightLeg.FindComponent<Transform3D>().SetLocalTransform(new Vector3(0, 0, 0), Quaternion.Identity, Vector3.One);
        }

        private Entity CreateCamera(Scene scene)
        {
            var cameraEntity = new Entity()
                                 .AddComponent(new Transform3D()
                                 { Position = new Vector3(10, 10, 10) })
                                 .AddComponent(new CameraBehavior());

            return cameraEntity;
        }

        
    }
}
