using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer
{
    public interface IAssetBundleCache
    {
        Shader FindShader(string name);
        Dictionary<string, Material> CreateMaterials();
    }
}
