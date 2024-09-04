using Evergine.Common.Graphics;
using Evergine.Components.Graphics3D;
using Evergine.Framework;
using Evergine.Framework.Graphics;
using Evergine.Framework.Services;
using Evergine.Mathematics;

namespace TrailsStudio
{
    public class MyScene : Scene
    {
        public override void RegisterManagers()
        {
            base.RegisterManagers();
            
            this.Managers.AddManager(new global::Evergine.Bullet.BulletPhysicManager3D());
            
        }

        protected override void CreateScene()
        {
            var assetsService = Application.Current.Container.Resolve<AssetsService>();

            var material = assetsService.Load<Material>(EvergineContent.Materials.DefaultMaterial);

            Entity plane = new Entity()
            .AddComponent(new Transform3D())
            .AddComponent(new MaterialComponent() { Material = material})
            .AddComponent(new PlaneMesh())
            .AddComponent(new MeshRenderer());

            this.Managers.EntityManager.Add(plane);
        }
    }
}


