using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace VesselViewer
{
    public interface IAssetBundleLoader
    {
        IEnumerator Initialize(string bundlePath);
        IAssetBundleCache GetResult();
    }
}
