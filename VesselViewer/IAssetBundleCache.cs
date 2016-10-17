using System.Collections.Generic;
using UnityEngine;

namespace VesselViewer
{
    public interface IAssetBundleCache
    {
        Shader FindShader(string name);
        Dictionary<string, Material> CreateMaterials();
    }
}