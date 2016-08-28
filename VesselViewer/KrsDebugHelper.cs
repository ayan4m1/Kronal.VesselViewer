using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace VesselViewer
{
    internal static class KrsDebugHelper
    {
        private static readonly Stack<KeyValuePair<string, Stopwatch>> dbgStack =
            new Stack<KeyValuePair<string, Stopwatch>>();

        public static T Module<T>(this Part part) where T : PartModule
        {
            return (T) part.Modules[typeof(T).Name];
        }

        public static void DbgBegin(this object self, string name = "")
        {
            var stopwatch = new Stopwatch();
            dbgStack.Push(new KeyValuePair<string, Stopwatch>(name, stopwatch));
            stopwatch.Start();
        }

        public static void DbgEnd(this object self)
        {
            var e = dbgStack.Pop();
            var name = e.Key;
            var stopwatch = e.Value;
            stopwatch.Stop();
            var ts = stopwatch.Elapsed;
            var elapsedTime = String.Format("{0:00}:{1:00}:{2:00}.{3:000}",
                ts.Hours, ts.Minutes, ts.Seconds,
                ts.Milliseconds);
            //MonoBehaviour.print("[DEBUG] Event: " + name + "   DT: " + elapsedTime);
        }
    }
}