using System;
using System.IO;
using BM;

namespace ET.Client
{
    [Event(SceneType.Process)]
    public class EntryEvent3_InitClient: AEvent<ET.EventType.EntryEvent3>
    {
        protected override async ETTask Run(Scene scene, ET.EventType.EntryEvent3 args)
        {
            // 加载配置
            // Root.Instance.Scene.AddComponent<ResourcesComponent>();
            AssetComponentConfig.DefaultBundlePackageName = "AllBundle";
            await AssetComponent.Initialize("AllBundle");
            Root.Instance.Scene.AddComponent<GlobalComponent>();

            // await ResourcesComponent.Instance.LoadBundleAsync("unit.unity3d");
            await AssetComponent.LoadAsync(BPath.Assets_Bundles_Unit_Unit__prefab);
            
            Scene clientScene = await SceneFactory.CreateClientScene(1, "Game");
            
            await EventSystem.Instance.PublishAsync(clientScene, new EventType.AppStartInitFinish());
        }
    }
}