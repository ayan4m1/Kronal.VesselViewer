using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using VesselViewer.Config;
using VesselViewer.Services;

namespace VesselViewer
{
    public class VesselShot : MonoBehaviour
    {
        private readonly CameraService _cameraService;
        private readonly IEffectService _effectService;
        private readonly IScreenshotService _screenshotService;
        private readonly IPartReplaceService _partReplaceService;

        public VesselShot(CameraService camera, IEffectService effect,
            IScreenshotService screenshot, IPartReplaceService partReplace)
        {
            Config = new VesselViewConfig();
            Direction = Vector3.forward;

            GameEvents.onPartAttach.Add(PartModified);
            GameEvents.onPartRemove.Add(PartModified);

            _cameraService = camera;
            _effectService = effect;
            _screenshotService = screenshot;
            _partReplaceService = partReplace;

            _cameraService.Initialize();

            // blueprint shader missing
            //Effects["Blueprint"].Enabled = false;

            // wat
            UiFloatVals["bgR"] = UiFloatVals["bgR_"];
            UiFloatVals["bgG"] = UiFloatVals["bgG_"];
            UiFloatVals["bgB"] = UiFloatVals["bgB_"];

            UpdateShipBounds();
        }

        public static readonly List<string> ShaderNames = new List<string>
        {
            "Hidden/EdgeDetect",
            "KSP/Alpha/Cutoff",
            "KSP/Diffuse",
            "KSP/Bumped",
            "KSP/Bumped Specular",
            "KSP/Specular",
            "KSP/Unlit",
            "KSP/Emissive/Specular",
            "KSP/Emissive/Bumped Specular"
        };

        internal Vector3 Direction;
        internal Vector3 Position;

        private Bounds _shipBounds;
        public Vector3 ShipSize => _shipBounds.size;

        public Dictionary<string, bool> UiBoolVals = new Dictionary<string, bool>
        {
            {"canPreview", true}
        };

        public Dictionary<string, float> UiFloatVals = new Dictionary<string, float>
        {
            {"shadowVal", 0f},
            {"shadowValPercent", 0f},
            {"imgPercent", 4f},
            {"bgR", 1f},
            {"bgG", 1f},
            {"bgB", 1f},
            {"bgA", 1f}, //RGBA
            {"bgR_", 0f},
            {"bgG_", 0.07f},
            {"bgB_", 0.11f},
            {"bgA_", 1f} //RGBA defaults //00406E 0,64,110 -> reduced due to color adjust shader
        };

        internal VesselViewConfig Config { get; }

        internal IShipconstruct Ship
        {
            get
            {
                if (!EditorLogic.RootPart)
                {
                    return null;
                }

                return EditorLogic.fetch ? EditorLogic.fetch.ship : null;
            }
        }

        ~VesselShot()
        {
            GameEvents.onPartAttach.Remove(PartModified);
            GameEvents.onPartRemove.Remove(PartModified);
        }

        // Different rotations for SPH and VAB
        public void RotateShip(float degrees)
        {
            Vector3 rotateAxis;

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                Debug.Log($"Rotating in SPH: {degrees}");
                //rotateAxis = EditorLogic.startPod.transform.forward;
                rotateAxis = EditorLogic.RootPart.transform.forward;
            }
            else
            {
                Debug.Log($"Rotating in VAB: {degrees}");
                rotateAxis = EditorLogic.RootPart.transform.up;
            }

            Direction = Quaternion.AngleAxis(degrees, rotateAxis)*Direction;
        }

        private void PartModified(GameEvents.HostTargetAction<Part, Part> data)
        {
            UpdateShipBounds();
        }

        internal void UpdateShipBounds()
        {
            if ((Ship != null) && (Ship.Parts.Count > 0))
                _shipBounds = CalcShipBounds();
            else
                _shipBounds = new Bounds(EditorLogic.fetch.editorBounds.center, Vector3.zero);
            
            _shipBounds.Expand(1f);
        }

        private Bounds CalcShipBounds()
        {
            var result = new Bounds(Ship.Parts[0].transform.position, Vector3.zero);
            foreach (var current in Ship.Parts)
                if (current.collider && !current.Modules.Contains("LaunchClamp"))
                    result.Encapsulate(current.collider.bounds);
            return result;
        }

        public void GenTexture(Vector3 direction, int width, int height)
        {
            var isDrawing = UiBoolVals["canPreview"];

            var bounds = new Bounds()
            {
                size = new Vector3(width, height)
            };

            // todo: blueprint shader missing
            /*if (Effects["Blueprint"].Enabled)
                Camera.backgroundColor = new Color(1f, 1f, 1f, 0.0f);
            else*/
            var bgColor = new Color(UiFloatVals["bgR"], UiFloatVals["bgG"], UiFloatVals["bgB"],
                UiFloatVals["bgA"]);

            _cameraService.Update(bgColor, direction, bounds);

            if (isDrawing)
            {
                foreach (var part in EditorLogic.fetch.ship)
                    _partReplaceService.Replace(part);

                var texture = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);

                _cameraService.Camera.targetTexture = texture;
                _cameraService.Camera.depthTextureMode = DepthTextureMode.DepthNormals;
                _cameraService.Camera.Render();
                _cameraService.Camera.targetTexture = null;

                _effectService.ApplyEnabled(texture);

                _screenshotService.Save(texture, UiFloatVals["imgPercent"]);

                RenderTexture.ReleaseTemporary(texture);

                foreach (var part in EditorLogic.fetch.ship)
                    _partReplaceService.Restore(part);
            }

            Resources.UnloadUnusedAssets();
        }

        public void Explode()
        {
            if (!EditorLogic.RootPart || (Ship == null))
                return;

            Config.Execute(Ship);
            UpdateShipBounds();
        }

        public void Update(int width = -1, int height = -1)
        {
            if (!EditorLogic.RootPart || (Ship == null))
                return;

            var dir = EditorLogic.RootPart.transform.TransformDirection(Direction);
            var shadowDist = QualitySettings.shadowDistance;

            QualitySettings.shadowDistance = UiFloatVals["shadowVal"] < 0f ? 0f : UiFloatVals["shadowVal"];

            GenTexture(dir, width, height);

            QualitySettings.shadowDistance = shadowDist;
        }
    }
}