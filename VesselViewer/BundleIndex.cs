using System.Collections.Generic;
using System.IO;
using System.Reflection;
using KSPAssets;
using KSPAssets.Loaders;
using UniLinq;
using UnityEngine;

namespace VesselViewer
{
    public class BundleIndex
    {
        internal static Dictionary<string, Font> Fonts = new Dictionary<string, Font>();
        internal static Dictionary<string, Shader> Shaders = new Dictionary<string, Shader>();
        
        public BundleIndex()
        {
            AssetLoader.LoadedBundleDefinitions.ForEach((bundle) => Debug.Log(bundle.path));

            var shaderDefs = AssetLoader.GetAssetDefinitionsWithType("vesselviewer", typeof(Shader));
            if (shaderDefs == null || shaderDefs.Length == 0)
            {
                Debug.Log("KVV: Failed to load Asset Package.");
            }
            else
            {
                AssetLoader.LoadAssets(ShadersLoaded, shaderDefs[0]);
            }
        }

        public Shader getShaderById(string idIn)
        {
            return (Shaders.ContainsKey(idIn) ? Shaders[idIn] : null);
        }

        private void ShadersLoaded(AssetLoader.Loader loader) // thanks moarDV - https://github.com/Mihara/RasterPropMonitor/blob/5c9fa8b259dd391892fe121724519413ccbb6b59/RasterPropMonitor/Core/UtilityFunctions.cs
        {
#if DEBUG
            Debug.Log(string.Format("KVV: ShadersLoaded"));
#endif
            string aShaderName = string.Empty;
            for (int i = 0; i < loader.objects.Length; ++i)
            {
                Object o = loader.objects[i];
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

            var loadedBundles = AssetLoader.LoadedBundles;
            if (loadedBundles == null)
            {
                Debug.Log("KVV: Unable to find any loaded bundles in AssetLoader.");
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
                        Shaders[shaders[j].name] = shaders[j];
                    }
                    for (int j = 0; j < fonts.Length; ++j)
                    {
#if DEBUG
                        Debug.Log(string.Format("KVV: Adding KSP-Bundle-included font {0} / {1}", fonts[j].name, fonts[j].fontSize));
#endif
                        Fonts[fonts[j].name] = fonts[j];
                    }
                    return;
                }
            }
        }
    }

}
