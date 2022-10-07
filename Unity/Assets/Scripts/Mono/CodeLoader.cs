using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BM;
using HybridCLR;
using UnityEngine;

namespace ET
{
	public class CodeLoader: Singleton<CodeLoader>
	{
		private Assembly assembly;

		public void Start()
		{
			if (Define.EnableCodes)
			{
				if (Init.Instance.GlobalConfig.CodeMode != CodeMode.ClientServer)
				{
					throw new Exception("ENABLE_CODES mode must use ClientServer code mode!");
				}
				
				Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
				Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(assemblies);
				EventSystem.Instance.Add(types);
				foreach (Assembly ass in assemblies)
				{
					string name = ass.GetName().Name;
					if (name == "Unity.Model.Codes")
					{
						this.assembly = ass;
					}
				}
				
				IStaticMethod start = new StaticMethod(assembly, "ET.Entry", "Start");
				start.Run();
			}
			else
			{
#if !UNITY_EDITOR
				LoadMetadataForAOTAssemblies();
#endif
				
				byte[] assBytes;
				byte[] pdbBytes;
				if (!Define.IsEditor)
				{
					// Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
					// assBytes = ((TextAsset)dictionary["Model.dll"]).bytes;
					// pdbBytes = ((TextAsset)dictionary["Model.pdb"]).bytes;
					assBytes = AssetComponent.Load<TextAsset>("Assets/Bundles/Code/Model.dll.bytes").bytes;
					pdbBytes = AssetComponent.Load<TextAsset>("Assets/Bundles/Code/Model.pdb.bytes").bytes;
				}
				else
				{
					assBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, "Model.dll"));
					pdbBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, "Model.pdb"));
				}
			
				assembly = Assembly.Load(assBytes, pdbBytes);
				this.LoadHotfix();

				IStaticMethod start = new StaticMethod(assembly, "ET.Entry", "Start");
				start.Run();
			}
		}

		// 热重载调用该方法
		public void LoadHotfix()
		{
			byte[] assBytes;
			byte[] pdbBytes;
			if (!Define.IsEditor)
			{
				// Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
				// assBytes = ((TextAsset)dictionary["Hotfix.dll"]).bytes;
				// pdbBytes = ((TextAsset)dictionary["Hotfix.pdb"]).bytes;
				assBytes = AssetComponent.Load<TextAsset>("Assets/Bundles/Code/Hotfix.dll.bytes").bytes;
				pdbBytes = AssetComponent.Load<TextAsset>("Assets/Bundles/Code/Hotfix.pdb.bytes").bytes;
			}
			else
			{
				// 傻屌Unity在这里搞了个傻逼优化，认为同一个路径的dll，返回的程序集就一样。所以这里每次编译都要随机名字
				string[] logicFiles = Directory.GetFiles(Define.BuildOutputDir, "Hotfix_*.dll");
				if (logicFiles.Length != 1)
				{
					throw new Exception("Logic dll count != 1");
				}
				string logicName = Path.GetFileNameWithoutExtension(logicFiles[0]);
				assBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, $"{logicName}.dll"));
				pdbBytes = File.ReadAllBytes(Path.Combine(Define.BuildOutputDir, $"{logicName}.pdb"));
			}

			Assembly hotfixAssembly = Assembly.Load(assBytes, pdbBytes);
			
			Dictionary<string, Type> types = AssemblyHelper.GetAssemblyTypes(typeof (Game).Assembly, this.assembly, hotfixAssembly);
			
			EventSystem.Instance.Add(types);
		}
		
		private static void LoadMetadataForAOTAssemblies()
		{
			// 可以加载任意aot assembly的对应的dll。但要求dll必须与unity build过程中生成的裁剪后的dll一致，而不能直接使用原始dll。
			// 我们在BuildProcessors里添加了处理代码，这些裁剪后的dll在打包时自动被复制到 {项目目录}/HybridCLRData/AssembliesPostIl2CppStrip/{Target} 目录。

			/// 注意，补充元数据是给AOT dll补充元数据，而不是给热更新dll补充元数据。
			/// 热更新dll不缺元数据，不需要补充，如果调用LoadMetadataForAOTAssembly会返回错误
			///
			List<string> aotDlls = Resources.Load<GlobalConfig>("GlobalConfig").AOTMetaAssemblyNames;
			foreach (var aotDllName in aotDlls)
			{
				byte[] dllBytes = AssetComponent.Load<TextAsset>($"Assets/Bundles/Aot/{aotDllName}.bytes").bytes;
				// 加载assembly对应的dll，会自动为它hook。一旦aot泛型函数的native函数不存在，用解释器版本代码
				LoadImageErrorCode err = RuntimeApi.LoadMetadataForAOTAssembly(dllBytes);
				Debug.Log($"LoadMetadataForAOTAssembly:{aotDllName}. ret:{err}");
			}
		}
	}
}