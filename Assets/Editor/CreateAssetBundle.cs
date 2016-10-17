using UnityEngine;
using UnityEditor;
using System.Collections;

public class AssetBundleCompiler
{
    internal static BuildAssetBundleOptions Options = BuildAssetBundleOptions.UncompressedAssetBundle |
                                                      BuildAssetBundleOptions.ForceRebuildAssetBundle;
    [MenuItem("Assets/Build AssetBundles OSX")]
    static void BuildAllAssetBundlesOSX()
    {
        BuildPipeline.BuildAssetBundles("AssetBundles", Options, BuildTarget.StandaloneOSXUniversal);
    }

    [MenuItem("Assets/Build AssetBundles Lin")]
    static void BuildAllAssetBundlesLin()
    {
        BuildPipeline.BuildAssetBundles("AssetBundles", Options, BuildTarget.StandaloneLinux);
    }

    [MenuItem("Assets/Build AssetBundles Win32")]
    static void BuildAllAssetBundlesWin32()
    {
        BuildPipeline.BuildAssetBundles("AssetBundles", Options, BuildTarget.StandaloneWindows);
    }

    [MenuItem("Assets/Build AssetBundles Win64")]
    static void BuildAllAssetBundlesWin64()
    {
        BuildPipeline.BuildAssetBundles("AssetBundles", Options, BuildTarget.StandaloneWindows64);
    }
}
