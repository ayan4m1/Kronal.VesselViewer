using System.Collections;

namespace VesselViewer.Assets
{
    public interface IAssetBundleLoader
    {
        IEnumerator Initialize(string bundlePath);
        IAssetBundleCache GetResult();
    }
}