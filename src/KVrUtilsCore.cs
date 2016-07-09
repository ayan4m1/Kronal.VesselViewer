using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAssets;

namespace KronalUtils
{
    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    class KVrUtilsCore : MonoBehaviour
    {
        public BundleIndex AssetIndex = new BundleIndex();
        public readonly string ModPath = Path.Combine(System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).ToString() + Path.DirectorySeparatorChar + "GameData" + Path.DirectorySeparatorChar, "KronalUtils");
        public readonly string SavePath = Path.Combine(System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).ToString(), "Screenshots");
        void Start()
        {

        }
        public string ModRoot()
        {
            return this.ModPath;
        }
        public string ModExport()
        {
            return this.SavePath;
        }
        public Shader getShaderById(string idIn)
        {
            return AssetIndex.gettShaderById(idIn);
        }

    }
}
