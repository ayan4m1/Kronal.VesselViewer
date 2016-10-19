using System.Collections.Generic;

namespace VesselViewer
{
    //taken from: https://github.com/Alewx/unofficailUbioWeld/tree/Beta -> https://github.com/Alewx/unofficailUbioWeld/blob/Beta/UbioWeldingLtd/WeldingHelpers.cs
    public static class EditorLockManager
    {
        private static readonly List<EditorLock> ActiveLocks = new List<EditorLock>();

        /// <summary>
        ///     locks the editor keys for the given key
        /// </summary>
        /// <param name="loadButton"></param>
        /// <param name="exitButton"></param>
        /// <param name="saveButton"></param>
        /// <param name="lockKey"></param>
        public static void LockEditor(bool loadButton, bool exitButton, bool saveButton, string lockKey)
        {
            if (IsLockKeyActive(lockKey)) return;
            EditorLogic.fetch.Lock(loadButton, exitButton, saveButton, lockKey);
            ActiveLocks.Add(new EditorLock(loadButton, exitButton, loadButton, lockKey));
        }


        /// <summary>
        ///     unlocks the editor for the entered key
        /// </summary>
        /// <param name="lockKey"></param>
        public static void UnlockEditor(string lockKey)
        {
            if (!IsLockKeyActive(lockKey)) return;
            EditorLogic.fetch.Unlock(lockKey);

            for (var i = 0; i < ActiveLocks.Count; i++)
                if (ActiveLocks[i].LockKey == lockKey)
                {
                    ActiveLocks.RemoveAt(i);
                    return;
                }
        }


        /// <summary>
        ///     returns the info about the current lockstatus
        /// </summary>
        /// <returns></returns>
        public static bool IsEditorLocked() => ActiveLocks.Count > 0;


        /// <summary>
        ///     provides all the keys that are currently in use
        /// </summary>
        /// <returns></returns>
        public static string[] GetActiveLockKeys()
        {
            var locks = new string[ActiveLocks.Count];
            for (var i = 0; i < locks.Length; i++)
                locks[i] = ActiveLocks[i].LockKey;
            return locks;
        }


        /// <summary>
        ///     provides the binary information if the key is already in use
        /// </summary>
        /// <param name="lockKey"></param>
        /// <returns></returns>
        public static bool IsLockKeyActive(string lockKey)
        {
            foreach (var l in ActiveLocks)
                if (l.LockKey == lockKey)
                    return true;
            return false;
        }


        /// <summary>
        ///     provides the information if the main buttons of the editor are locked
        /// </summary>
        /// <returns></returns>
        public static bool IsEditorSoftlocked()
        {
            foreach (var l in ActiveLocks)
                if (l.LockSave && l.LockExit && l.LockLoad)
                    return true;
            return false;
        }


        /// <summary>
        ///     resets the editorlocks to a clean state
        /// </summary>
        public static void ResetEditorLocks()
        {
            ActiveLocks.Clear();
        }

        public class EditorLock
        {
            public EditorLock(bool save, bool exit, bool load, string key)
            {
                LockSave = save;
                LockExit = exit;
                LockLoad = load;
                LockKey = key;
            }

            public bool LockSave { get; set; }

            public bool LockExit { get; set; }

            public bool LockLoad { get; set; }

            public string LockKey { get; set; }
        }
    }
}