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
        const int FLOOR_SIZE = 100;
        const float ROLL_IN_SIZE = 1.5f;
        const float LEG_DIAMETER = 0.2f;

        private int rollInHeight;
        private int rollInAngle;

        private Entity floor;

        protected override void Start()
        {
            base.Start();
            
            var assetsService = Application.Current.Container.Resolve<AssetsService>();
            
            var baseScene = assetsService.Load<MyScene>(EvergineContent.Scenes.MyScene_wescene);

            floor = CreateFloor(baseScene, assetsService, FLOOR_SIZE, FLOOR_SIZE);

            baseScene.Managers.EntityManager.Add(floor);

            CreateRollIn(baseScene, assetsService, rollInAngle, rollInHeight);

            CreateCamera(baseScene);
        }

        public void RegisterRollInParams(int rollInHeight, int rollInAngle)
        {
            this.rollInHeight = rollInHeight;
            this.rollInAngle = rollInAngle;
        }

        private Entity CreateFloor(Scene scene, AssetsService assetsService, int width, int height) 
        {
            var grassMaterial = assetsService.Load<Material>(EvergineContent.Materials.grassMaterial);

            Entity floor = new Entity("floor")
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
            var plankMaterial = assetsService.Load<Material>(EvergineContent.Materials.woodenPlankMaterial);

            float[] xAdders = { ROLL_IN_SIZE, 0, ROLL_IN_SIZE, 0 };
            float[] zAdders = { ROLL_IN_SIZE, ROLL_IN_SIZE, 0, 0 };

            for (int i = 0; i < 4; i++)
            {
                // y-axis is half of height, because a cylinder mesh is created from the center
                var position = new Vector3(xAdders[i], height/2, zAdders[i]);

                var rollInLeg = new Entity()
                 .AddComponent(new Transform3D() { LocalPosition = position })
                 .AddComponent(new MaterialComponent() { Material = legMaterial })
                 .AddComponent(new CylinderMesh() { Diameter = LEG_DIAMETER, Height = height })
                 .AddComponent(new MeshRenderer())
                 .AddComponent(new StaticBody3D());

                rollInLeg.Tag = "rollInLeg";

                floor.AddChild(rollInLeg);
            }

            var localTopPosition = new Vector3(ROLL_IN_SIZE / 2, height, ROLL_IN_SIZE / 2);

            var rollInTop = new Entity()
            .AddComponent(new Transform3D() { LocalPosition = localTopPosition })
            .AddComponent(new MaterialComponent() { Material = plankMaterial })
            .AddComponent(new PlaneMesh() { Width = ROLL_IN_SIZE + LEG_DIAMETER, Height = ROLL_IN_SIZE + LEG_DIAMETER, TwoSides = true })
            .AddComponent(new MeshRenderer())
            .AddComponent(new StaticBody3D());

            rollInTop.Tag = "rollInTop";

            floor.AddChild(rollInTop);
        }

        private void CreateCamera(Scene scene)
        {
            var cameraEntity = new Entity("camera")
                                 .AddComponent(new Transform3D())
                                 .AddComponent(new CameraBehavior());

            scene.Managers.EntityManager.Add(cameraEntity);
        }


    }
}
