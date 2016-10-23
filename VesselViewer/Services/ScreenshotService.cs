using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VesselViewer.Services
{
    public class ScreenshotService : IScreenshotService
    {
        private CameraService _cameraService;

        public ScreenshotService(CameraService camera)
        {
            _cameraService = camera;
        }

        public void Save(RenderTexture texture, float scalar = 1f)
        {
            var fileWidth = texture.width;
            var fileHeight = texture.height;

            if (scalar >= 1f)
            {
                fileWidth = (int)Math.Floor(fileWidth * scalar);
                fileHeight = (int)Math.Floor(fileHeight * scalar);
            }
#if DEBUG
            Debug.Log(string.Format("KVV: SIZE: {0} x {1}", fileWidth, fileHeight));
#endif

            var screenShot = new Texture2D(fileWidth, fileHeight, TextureFormat.ARGB32, false);

            var saveRt = RenderTexture.active;
            RenderTexture.active = texture;
            screenShot.ReadPixels(new Rect(0, 0, fileWidth, fileHeight), 0, 0);
            screenShot.Apply();
            RenderTexture.active = saveRt;
            var bytes = screenShot.EncodeToPNG();
            var shipNameFileSafe = GetShipName();
            uint fileInc = 0;
            var filename = "";

            do
            {
                ++fileInc;
                var filenamebase = shipNameFileSafe + "_" + fileInc + ".png";
                filename = Path.Combine(Directory.GetParent(KSPUtil.ApplicationRootPath).ToString(),
                    "Screenshots" + Path.DirectorySeparatorChar + filenamebase);
            } while (File.Exists(filename));

            File.WriteAllBytes(filename, bytes);
            Debug.Log($"KVV: Took screenshot to: {filename}");

            //DestroyObject(screenShot);
        }

        private string GetShipName()
        {
            if (EditorLogic.fetch && (EditorLogic.fetch.ship != null))
            {
                var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
                var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
                return Regex.Replace(EditorLogic.fetch.ship.shipName, invalidRegStr, "_");
            }
            return "vessel";
        }
    }
}
