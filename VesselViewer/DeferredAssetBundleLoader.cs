using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using KSPAssets.Loaders;
using UnityEngine;

namespace VesselViewer
{
    public class DeferredAssetBundleLoader : IAssetBundleLoader
    {
        private IAssetBundleCache _result;

        public IAssetBundleCache GetResult()
        {
            return _result;
        }

        public IEnumerator Initialize(string bundlePath)
        {
            Debug.Log("KVV: Searching for asset bundle");
            var shaderDefs = AssetLoader.GetAssetDefinitionsWithType(bundlePath, typeof(Shader));
            if ((shaderDefs == null) || (shaderDefs.Length == 0))
            {
                Debug.Log("KVV: Failed to find asset bundle!");
                yield return new WaitForEndOfFrame();
            }

            var shaderDef = shaderDefs[0];
            var bundle = shaderDef.bundle;
            Debug.Log("KVV: Passing " + (bundle?.assets?.Count ?? 0) + " asset definitions");
            if (bundle?.assets == null)
            {
                Debug.Log("KVV: Unexpected empty asset bundle!");
                yield return new WaitForEndOfFrame();
            }

#if DEBUG
            foreach (var def in bundle.assets)
            {
                Debug.Log("KVV: " + def.name + " @ " + def.path);
            }
#endif

            AssetLoader.LoadAssets(Initialized, shaderDef);

            // continue to check
            yield return new WaitUntil(() => _result != null);
        }

        public void Initialized(AssetLoader.Loader loader)
        // thanks moarDV - https://github.com/Mihara/RasterPropMonitor/blob/5c9fa8b259dd391892fe121724519413ccbb6b59/RasterPropMonitor/Core/UtilityFunctions.cs
        {
            var cache = new AssetBundleCache();
            Debug.Log("KVV: Cache is being initialized");

            var aShaderName = string.Empty;
            for (var i = 0; i < loader.objects.Length; ++i)
            {
                var o = loader.objects[i];
                if ((o != null) && o is Shader)
                {
                    // We'll remember the name of whichever shader we were
                    // able to load.
                    aShaderName = o.name;
                    break;
                }
            }

            if (string.IsNullOrEmpty(aShaderName))
            {
                Debug.Log(string.Format("KVV: Unable to find a shader named \"{0}\".", aShaderName));
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
            for (var i = 0; i < loadedBundles.Count; ++i)
            {
                Shader[] shaders = null;
                var theRightBundle = false;

                try
                {
                    // Try to get a list of all the shaders in the bundle.
                    shaders = loadedBundles[i].LoadAllAssets<Shader>();
                    if (shaders != null && shaders.Any(t => t.name == aShaderName))
                    {
                        theRightBundle = true;
                    }
                }
                catch
                {
                    Debug.Log("KVV: Exception ended our shader search for bundle " + i);
                }

                if (theRightBundle)
                {
                    // If we found our bundle, set up our parsedShaders
                    // dictionary and bail - our mission is complete.
                    for (var j = 0; j < shaders.Length; ++j)
                    {
#if DEBUG
                        if (!shaders[j].isSupported)
                        {
                            Debug.Log(string.Format("KVV: Shader {0} - unsupported in this configuration",
                                shaders[j].name));
                        }
#endif

                        Debug.Log("KVV: Setting shader key " + shaders[j].name);
                        cache.Shaders[shaders[j].name] = shaders[j];
                    }

                    Debug.Log("KVV: Cached " + cache.Shaders.Count + " shaders...");
                }
            }

            _result = cache;
        }
    }
}
