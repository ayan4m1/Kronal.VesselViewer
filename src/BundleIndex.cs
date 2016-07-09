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
    class BundleIndex
    {
        //KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
        /*
        public Dictionary<string, string> ShaderData = new Dictionary<string, string> { 
            { "MaterialFXAA", KronalUtils.Properties.Resources.ShaderFXAA }, 
            { "MaterialColorAdjust", KSP.IO.File.ReadAllText<KVrVesselShot>("coloradjust") }, 
            { "MaterialEdgeDetect", KSP.IO.File.ReadAllText<KVrVesselShot>("edn2") }, 
            { "MaterialBluePrint", KSP.IO.File.ReadAllText<KVrVesselShot>("blueprint") }, 
        };*/
        internal static Dictionary<string, Font> loadedFonts = new Dictionary<string, Font>();
        internal static Dictionary<string, Shader> loadedShaders = new Dictionary<string, Shader>();
        public Dictionary<string, string> ShaderData = new Dictionary<string, string> {
            { "MaterialFXAA", "KVV/Hidden/SlinDev/Desktop/PostProcessing/FXAA" },
            { "MaterialColorAdjust", "KVV/Color Adjust" },
            { "MaterialEdgeDetect", "KVV/Hidden/Edge Detect Normals2" },
        };
        public Dictionary<string, KSPAssets.Loaders.AssetLoader> KVrShaders = new Dictionary<string, KSPAssets.Loaders.AssetLoader>();
        public Dictionary<string, KSPAssets.Loaders.AssetLoader> KVrShaders2 = new Dictionary<string, KSPAssets.Loaders.AssetLoader>();
        public BundleIndex()
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
            Debug.Log(string.Format("KVV: InitShaders 1: {0}", KSPAssets.Loaders.AssetLoader.ApplicationRootPath));
#endif
            
            string KVrPath = KVrUtilsCore.ModRoot();
            string KVrAssetPath = Path.GetDirectoryName(KVrPath + Path.DirectorySeparatorChar);
#if DEBUG
            Debug.Log(string.Format("KVV: InitShaders 2 KVrPath{0} \n\t- Directory  Path.GetFileName( KVrAssetPath ...) {1} ", KVrPath, Path.GetFileName(KVrAssetPath + Path.DirectorySeparatorChar + "kvv")));//, String.Join("\n\t- ", System.IO.Directory.GetFiles(KVrPath))
#endif
            KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType("KronalUtils/kvv", typeof(Shader));//path to kvv.ksp
            if (KVrShaders == null || KVrShaders.Length == 0)
            {
                Debug.Log(string.Format("KVV: Failed to load Asset Package KronalUtils/kvv.ksp in {0}.", KVrPath));
                return;
            }else {
                KSPAssets.Loaders.AssetLoader.LoadAssets(ShadersLoaded, KVrShaders[0]);
            }
            
#if DEBUG
            Debug.Log(string.Format("KVV: InitShaders 4"));
#endif
            /*
            foreach (KeyValuePair<string, string> itKey in ShaderData)
            {
                //KSPAssets.AssetDefinition[itKey.Key] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
                //KSPAssets.Loaders.AssetLoader newShad = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(ShaderData[itKey.Key], typeof(Shader));
                //KVrShaders.Add(itKey.Key, new KSPAssets.Loaders.AssetLoader());
                //KVrShaders[itKey.Key] = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(ShaderData[itKey.Key], typeof(UnityEngine.Shader));
                // KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
            }*/
        }
        public Shader gettShaderById(string idIn)
        {
            return (loadedShaders.ContainsKey(idIn) ? loadedShaders [idIn] : null);
        }
        private void ShadersLoaded(KSPAssets.Loaders.AssetLoader.Loader loader) // thanks moarDV - https://github.com/Mihara/RasterPropMonitor/blob/5c9fa8b259dd391892fe121724519413ccbb6b59/RasterPropMonitor/Core/UtilityFunctions.cs
        {
#if DEBUG
            Debug.Log(string.Format("KVV: ShadersLoaded"));
#endif
            string aShaderName = string.Empty;
            for (int i = 0; i < loader.objects.Length; ++i)
            {
                UnityEngine.Object o = loader.objects[i];
                if (o != null && o is Shader)
                {
                    // We'll remember the name of whichever shader we were
                    // able to load.
                    aShaderName = o.name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(aShaderName))
            {
                Debug.Log(string.Format("KVV: Unable to find a named shader \"{0}\".", aShaderName));
                return;
            }

            var loadedBundles = KSPAssets.Loaders.AssetLoader.LoadedBundles;
            if (loadedBundles == null)
            {
                Debug.Log(string.Format("KVV: Unable to find any loaded bundles in AssetLoader."));
                return;
            }

            // Iterate over all loadedBundles.  Experimentally, my bundle was
            // the only one in the array, but I expect that to change as other
            // mods use asset bundles (maybe none of the mods I have load this
            // early).
            for (int i = 0; i < loadedBundles.Count; ++i)
            {
                Shader[] shaders = null;
                Font[] fonts = null;
                bool theRightBundle = false;

                try
                {
                    // Try to get a list of all the shaders in the bundle.
                    shaders = loadedBundles[i].LoadAllAssets<Shader>();
                    if (shaders != null)
                    {
                        // Look through all the shaders to see if our named
                        // shader is one of them.  If so, we assume this is
                        // the bundle we want.
                        for (int shaderIdx = 0; shaderIdx < shaders.Length; ++shaderIdx)
                        {
                            if (shaders[shaderIdx].name == aShaderName)
                            {
                                theRightBundle = true;
                                break;
                            }
                        }
                    }
                    fonts = loadedBundles[i].LoadAllAssets<Font>();
                }
                catch { }

                if (theRightBundle)
                {
                    // If we found our bundle, set up our parsedShaders
                    // dictionary and bail - our mission is complete.
                    for (int j = 0; j < shaders.Length; ++j)
                    {
                        if (!shaders[j].isSupported)
                        {
#if DEBUG
                            Debug.Log(string.Format("KVV: Shader {0} - unsupported in this configuration", shaders[j].name));
#endif
                        }
                        loadedShaders[shaders[j].name] = shaders[j];
                    }
                    for (int j = 0; j < fonts.Length; ++j)
                    {
#if DEBUG
                        Debug.Log(string.Format("KVV: Adding KSP-Bundle-included font {0} / {1}", fonts[j].name, fonts[j].fontSize));
#endif
                        loadedFonts[fonts[j].name] = fonts[j];
                    }
                    return;
                }
            }

            Debug.Log(string.Format("KVV: Failed to load shaders  - how did this callback execute?"));
        }
    }

}
