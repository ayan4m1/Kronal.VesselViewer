using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VesselViewer
{
    public class AssetBundleCache : IAssetBundleCache
    {
        public Dictionary<string, Shader> Shaders;

        public AssetBundleCache()
        {
            Shaders = new Dictionary<string, Shader>();
        }

        public Dictionary<string, Material> CreateMaterials()
        {
            return Shaders.ToDictionary((pair) => pair.Key, (pair) => new Material(pair.Value));
        }

        public Shader FindShader(string id)
        {
            return Shaders.ContainsKey(id) ? Shaders[id] : null;
        }
    }
}