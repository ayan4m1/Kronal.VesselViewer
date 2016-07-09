namespace UnityEditor
{
    public class CreateAssetBundle
    {
        [MenuItem("Tests/Create/Create From Asset Database")]
        public static void CreateNewAssetBundleFromAssetDatabase()
        {
            string outputPath = "AssetBundles";
            //AssetBundleBuild[] buildOpts = new AssetBundleBuild[2];
            //buildOpts.assetNames;
            //buildOpts.assetBundleVariant;
            //buildOpts.assetBundleName;
            //AssetBundleBuild.assetNames AssetBundleBuild.assetBundleVariant AssetBundleBuild.assetBundleName
            //EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTarget.StandaloneWindows);//BuildTarget.StandaloneWindows BuildTarget.Android , BuildAssetBundleOptions.None, BuildTarget.StandaloneOSXUniversal
            BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.UncompressedAssetBundle  | BuildAssetBundleOptions.ForceRebuildAssetBundle);
            //BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.UncompressedAssetBundle | BuildAssetBundleOptions.DeterministicAssetBundle | BuildAssetBundleOptions.ForceRebuildAssetBundle);
            //BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.UncompressedAssetBundle);
            //BuildPipeline.BuildAssetBundles(outputPath, BuildAssetBundleOptions.UncompressedAssetBundle, EditorUserBuildSettings.activeBuildTarget);//
        }
    }

}
