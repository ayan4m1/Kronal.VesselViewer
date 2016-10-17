using System.Collections;

namespace VesselViewer
{
    public interface IAssetBundleLoader
    {
        IEnumerator Initialize(string bundlePath);
        IAssetBundleCache GetResult();
    }
}