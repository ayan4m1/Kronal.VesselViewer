using System.Collections.Generic;
using UnityEngine;

namespace VesselViewer.Assets
{
    public interface IAssetBundleCache
    {
        Shader FindShader(string name);
        Dictionary<string, Material> CreateMaterials();
    }
}