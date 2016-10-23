using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VesselViewer.Assets;

namespace VesselViewer
{
    internal class KrsUtils
    {
        public static IAssetBundleCache AssetBundle;
        public static Dictionary<string, Material> MaterialCache;

        public static IEnumerator LoadBundle()
        {
            // asynchronously load the asset bundle shaders into a cache
            Debug.Log("KVV: Factory init");
            var factory = new AssetBundleCacheFactory();
            var loadRoutine = factory.LoadBundle("VesselViewer/Shaders/vesselviewer");
            Debug.Log("KVV: Load routine loop");

            while (loadRoutine.MoveNext())
            {
                yield return loadRoutine.Current;
            }

            Debug.Log("KVV: Load routine complete");
            AssetBundle = factory.Result;
            MaterialCache = AssetBundle.CreateMaterials();
        }

        public static Type FindType(string qualifiedTypeName)
        {
            var t = Type.GetType(qualifiedTypeName);

            if (t != null)
                return t;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                t = asm.GetType(qualifiedTypeName);
                if (t != null)
                    return t;
            }
            return null;
        }

        public static Vector3 ProjectVectorToPlane(Vector3 v, Vector3 planeNormal)
        {
            return v - Vector3.Dot(v, planeNormal)*planeNormal;
        }

        public static Vector3 VectorSwap(Vector3 v)
        {
            return new Vector3(v.y, v.z, v.x);
        }

        public static float VectorSignedAngle(Vector3 a, Vector3 b, Vector3 planeNormal)
        {
            var angle = Vector3.Angle(a, b);
            return Vector3.Dot(Vector3.Cross(planeNormal, a), b) >= 0f ? 360f - angle : angle;
        }

        public static float Wrap(float value, float min, float max)
        {
            return ((value - min)%(max - min) + (max - min))%(max - min) + min;
        }
    }
}