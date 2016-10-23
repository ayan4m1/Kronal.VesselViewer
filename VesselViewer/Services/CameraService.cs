using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer.Services
{
    public class CameraService : ICameraService
    {
        private Camera[] _cameras;

        public Camera Camera { get; private set; }

        public bool Orthographic
        {
            get
            {
                return Camera == _cameras[0];
                //if this currently selected camera is the first camera then Orthographic is true
            }
            set
            {
                Camera = _cameras[value ? 0 : 1];
                //if setting to true use the first camera (which is ortho camera). if false use the non-ortho
            }
        }

        public void Initialize()
        {
            _cameras = new Camera[2];
            _cameras[0] = new GameObject().AddComponent<Camera>();
            _cameras[0].enabled = false;
            _cameras[0].orthographic = true;
            _cameras[0].cullingMask = EditorLogic.fetch.editorCamera.cullingMask & ~(1 << 16); // hides kerbals
            _cameras[0].transparencySortMode = TransparencySortMode.Orthographic;
            _cameras[1] = new GameObject().AddComponent<Camera>();
            _cameras[1].enabled = false;
            _cameras[1].orthographic = false;
            _cameras[1].cullingMask = _cameras[0].cullingMask;
            Camera = _cameras[0];
        }

        public void Update(Color bgColor, Vector3 direction, Vector3 position, Bounds? imageSize, Bounds shipBounds)
        {
            var minusDir = -direction;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = bgColor;

            Camera.transform.position = shipBounds.center;

            // This sets the horizon before the camera looks to vehicle center.
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                Camera.transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
            else
                Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.right);
            // this.Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.up); // original 

            // Apply angle Vector to camera.
            Camera.transform.Translate(minusDir * Camera.nearClipPlane);
            // this.Camera.transform.Translate(Vector3.Scale(minusDir, this.shipBounds.extents) + minusDir * this.Camera.nearClipPlane); // original 
            // Deckblad: There was a lot of math here when all we needed to do is establish the rotation of the camera.

            // Face camera to vehicle.
            Camera.transform.LookAt(shipBounds.center);

            var tangent = Camera.transform.up;
            var binormal = Camera.transform.right;
            var height = Vector3.Scale(tangent, shipBounds.size).magnitude;
            var width = Vector3.Scale(binormal, shipBounds.size).magnitude;
            var depth = Vector3.Scale(minusDir, shipBounds.size).magnitude;

            width += Config.procFairingOffset; // get the distance of fairing offset
            depth += Config.procFairingOffset; // for the farClipPlane

            // Find distance from vehicle.
            var positionOffset = (shipBounds.size.magnitude - position.z) /
                                 (2f * Mathf.Tan(Mathf.Deg2Rad * Camera.fieldOfView / 2f));
            // float positionOffset = (height - this.position.z) / (2f * Mathf.Tan(Mathf.Deg2Rad * this.Camera.fieldOfView / 2f)) - depth * 0.5f; // original 
            // Use magnitude of bounds instead of height and remove vehicle bounds depth for uniform distance from vehicle. Height and depth of vehicle change in relation to the camera as we move around the vehicle.

            // Translate and Zoom camera
            Camera.transform.Translate(new Vector3(position.x, position.y, -positionOffset));

            // Get distance from camera to ship. Apply to farClipPlane
            var distanceToShip = Vector3.Distance(Camera.transform.position, shipBounds.center);

            // Set far clip plane to just past size of vehicle.
            Camera.farClipPlane = distanceToShip + Camera.nearClipPlane + depth * 2f + 1;
            // 1 for the first rotation vector
            // this.Camera.farClipPlane = Camera.nearClipPlane + positionOffset + this.position.magnitude + depth; // original

            if (Orthographic)
                Camera.orthographicSize = (Math.Max(height, width) - position.z) / 2f;

            int calcHeight, calcWidth;
            var tmpAspect = width / height;
            const int maxWidth = 1024, maxHeight = 1024;
            if (tmpAspect >= 1f)
            {
                calcHeight = maxHeight;
                calcWidth = (int)(calcHeight * tmpAspect);
            }
            else
            {
                calcWidth = maxWidth;
                calcHeight = (int)(calcWidth / tmpAspect);
            }

            // If we're saving, use full resolution.
            if (!imageSize.HasValue)
            {
                // Constrain image to max size with respect to aspect
                Camera.aspect = tmpAspect;
            }
            else
            {
                Camera.aspect = imageSize.Value.size.x / imageSize.Value.size.y;
            }
        }
    }
}
