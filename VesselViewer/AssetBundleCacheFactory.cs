using System.Collections;

namespace VesselViewer
{
    class AssetBundleCacheFactory
    {
        private static readonly IAssetBundleLoader Loader = new DeferredAssetBundleLoader();

        public IAssetBundleCache Result;
        private IEnumerator _loadRoutine;

        public IEnumerator LoadBundle(string bundlePath)
        {
            _loadRoutine = Loader.Initialize(bundlePath);

            // return our current iterator unless we are done loading
            while (_loadRoutine.MoveNext())
            {
                yield return _loadRoutine.Current;
            }

            // if done loading, return the cache via our public field
            var cache = Loader.GetResult();
            Result = cache;
        }
    }
}