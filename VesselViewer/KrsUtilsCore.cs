using System.IO;
using UnityEngine;

namespace VesselViewer
{
    [KSPAddon(KSPAddon.Startup.EditorAny, true)]
    class KrsUtilsCore : MonoBehaviour
    {
        public static BundleIndex AssetIndex = new BundleIndex();
        public static string ModPath = Path.Combine(Directory.GetParent(KSPUtil.ApplicationRootPath).ToString() + Path.DirectorySeparatorChar + "GameData" + Path.DirectorySeparatorChar, "VesselViewer");
        public static string SavePath = Path.Combine(Directory.GetParent(KSPUtil.ApplicationRootPath).ToString(), "Screenshots");
        public static string ModRoot()
        {
            return ModPath;
        }
        public static string ModExport()
        {
            return SavePath;
        }
    }
}
