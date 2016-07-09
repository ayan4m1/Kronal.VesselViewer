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
        public static BundleIndex AssetIndex = new BundleIndex();
        public static string ModPath = Path.Combine(System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).ToString() + Path.DirectorySeparatorChar + "GameData" + Path.DirectorySeparatorChar, "KronalUtils");
        public static string SavePath = Path.Combine(System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).ToString(), "Screenshots");
        public static string ModRoot()
        {
            return ModPath;
        }
        public static string ModExport()
        {
            return SavePath;
        }
        public static Shader getShaderById(string idIn)
        {
            return AssetIndex.gettShaderById(idIn);
        }

    }
}
