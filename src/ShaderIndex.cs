using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using UnityEngine;
using KSP;
using KSPAssets;

namespace KronalUtils
{
    class ShaderIndex
    {
        //KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
        public Dictionary<string, string> KVrShaderData = new Dictionary<string, string> { 
            { "MaterialFXAA", KronalUtils.Properties.Resources.ShaderFXAA }, 
            { "MaterialColorAdjust", KSP.IO.File.ReadAllText<KVrVesselShot>("coloradjust") }, 
            { "MaterialEdgeDetect", KSP.IO.File.ReadAllText<KVrVesselShot>("edn2") }, 
            { "MaterialBluePrint", KSP.IO.File.ReadAllText<KVrVesselShot>("blueprint") }, 
        };
        public Dictionary<string, KSPAssets.Loaders.AssetLoader> KVrShaders = new Dictionary<string, KSPAssets.Loaders.AssetLoader>(); 
        public ShaderIndex()
        {
            /*
            this.Effects = new Dictionary<string, ShaderMaterial>() {
                {"Color Adjust",MaterialColorAdjust},
                {"Edge Detect", MaterialEdgeDetect},
                {"Blue Print", MaterialBluePrint},
                {"FXAA", MaterialFXAA}
            };*/
            //KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
            


            InitShaders();
        }
        private void InitShaders()
        {

#if DEBUG
            Debug.Log(string.Format("KVV: InitShaders 1: {0}", Path.Combine(System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).ToString() + Path.DirectorySeparatorChar + "GameData" + Path.DirectorySeparatorChar, "KronalUtils")));
#endif
            string KVrPath = Path.Combine(System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).ToString() + Path.DirectorySeparatorChar + "GameData" + Path.DirectorySeparatorChar, "KronalUtils");
#if DEBUG
            Debug.Log(string.Format("KVV: InitShaders 2 {0}", KVrPath));
#endif
            KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType("kvv", typeof(Shader));
            //KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType("KronalUtils/kvv", typeof(Shader));
#if DEBUG
            Debug.Log(string.Format("KVV: InitShaders 3"));
#endif
            KSPAssets.Loaders.AssetLoader.LoadAssets(ShadersLoaded, KVrShaders[0]);
#if DEBUG
            Debug.Log(string.Format("KVV: InitShaders 4"));
#endif
            foreach (KeyValuePair<string, string> itKey in KVrShaderData)
            {
                //KSPAssets.AssetDefinition[itKey.Key] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
                //KSPAssets.Loaders.AssetLoader newShad = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KVrShaderData[itKey.Key], typeof(Shader));
                //KVrShaders.Add(itKey.Key, new KSPAssets.Loaders.AssetLoader());
                //KVrShaders[itKey.Key] = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KVrShaderData[itKey.Key], typeof(UnityEngine.Shader));
                // KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
            }
        }
        private void ShadersLoaded(KSPAssets.Loaders.AssetLoader.Loader loader)
        {
#if DEBUG
            Debug.Log(string.Format("KVV: ShadersLoaded"));
#endif
        }
    }

}
