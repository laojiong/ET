﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BM;
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
				byte[] assBytes;
				byte[] pdbBytes;
				if (!Define.IsEditor)
				{
					// Dictionary<string, UnityEngine.Object> dictionary = AssetsBundleHelper.LoadBundle("code.unity3d");
					// assBytes = ((TextAsset)dictionary["Model.dll"]).bytes;
					// pdbBytes = ((TextAsset)dictionary["Model.pdb"]).bytes;
					assBytes = AssetComponent.Load<TextAsset>("Bundles/Codes/Model.dll.bytes").bytes;
					pdbBytes = AssetComponent.Load<TextAsset>("Bundles/Codes/Model.pdb.bytes").bytes;
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
				assBytes = AssetComponent.Load<TextAsset>("Bundles/Codes/Hotfix.dll.bytes").bytes;
				pdbBytes = AssetComponent.Load<TextAsset>("Bundles/Codes/Hotfix.pdb.bytes").bytes;
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
	}
}