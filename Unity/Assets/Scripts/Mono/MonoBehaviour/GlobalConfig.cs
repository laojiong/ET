using System.Collections.Generic;
using UnityEngine;

namespace ET
{
    public enum CodeMode
    {
        Client = 1,
        Server = 2,
        ClientServer = 3,
    }
    
    [CreateAssetMenu(menuName = "ET/CreateGlobalConfig", fileName = "GlobalConfig", order = 0)]
    public class GlobalConfig: ScriptableObject
    {
        public CodeMode CodeMode;
        
        public List<string> AOTMetaAssemblyNames = new List<string>()
        {
            "mscorlib",
            "System",
            "System.Core", // 如果使用了Linq，需要这个
            "Unity.Core",
            "Unity.Mono",
            "Unity.ThirdParty",
            "MongoDB.Bson",
            "CommandLine",
            "NLog",

            //
            // 注意！修改这个列表请同步修改HotFix2模块中App.cs文件中的 LoadMetadataForAOTAssembly函数中aotDllList列表。
            // 两者需要完全一致
            //
        };
    }
}