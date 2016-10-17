using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VesselViewer
{
    public class VesselShot : MonoBehaviour
    {
        internal readonly Dictionary<string, ShaderMaterial> Effects = new Dictionary<string, ShaderMaterial>
        {
            ["Edge Detect"] = new ShaderMaterial("MaterialEdgeDetect", "Hidden/EdgeDetect"),
            ["FXAA"] = new ShaderMaterial("MaterialFXAA", "Hidden/FXAA3")
        };

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

        public int CalculatedHeight = 1;
        public int CalculatedWidth = 1;

        private Bounds _shipBounds;
        private Camera[] _cameras;
        private RenderTexture _texture;

        private readonly Dictionary<Part, Dictionary<MeshRenderer, Shader>> _partShaderLibrary =
            new Dictionary<Part, Dictionary<MeshRenderer, Shader>>();

        internal Vector3 Direction;
        internal Vector3 Position;
        internal float StoredShadowDistance;

        private const int MaxHeight = 1024;
        private const int MaxWidth = 1024;

        public Dictionary<string, bool> UiBoolVals = new Dictionary<string, bool>
        {
            {"canPreview", true},
            {"saveTextureEvent", false}
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

        public VesselShot()
        {
            Config = new VesselViewConfig();
            Direction = Vector3.forward;

            SetupCameras();

            // blueprint shader missing
            //Effects["Blueprint"].Enabled = false;
            // wat
            UiFloatVals["bgR"] = UiFloatVals["bgR_"];
            UiFloatVals["bgG"] = UiFloatVals["bgG_"];
            UiFloatVals["bgB"] = UiFloatVals["bgB_"];

            UpdateShipBounds();

            GameEvents.onPartAttach.Add(PartModified);
            GameEvents.onPartRemove.Add(PartModified);
        }

        internal Camera Camera { get; private set; }
        // keeps original shadow distance. Used to toggle shadows off during rendering.

        internal bool EffectsAntiAliasing { get; set; } //consider obsolete?

        internal bool Orthographic
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

        internal VesselViewConfig Config { get; }

        internal IShipconstruct Ship
        {
            get
            {
                if (EditorLogic.fetch)
                    return EditorLogic.fetch.ship;
                return null;
            }
        }

        internal string ShipName
        {
            get
            {
                if (EditorLogic.fetch && (EditorLogic.fetch.ship != null))
                    return MakeValidFileName(EditorLogic.fetch.ship.shipName);
                return "vessel";
            }
        }

        ~VesselShot()
        {
            GameEvents.onPartAttach.Remove(PartModified);
            GameEvents.onPartRemove.Remove(PartModified);
        }

        // Sets up Orthographic and Perspective camera.
        private void SetupCameras()
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

        private void ReplacePartShaders(Part part)
        {
            var materials = KrsUtils.MaterialCache;
            var model = part.transform.Find("model");
            if (!model) return;

            var meshLibrary = new Dictionary<MeshRenderer, Shader>();
            foreach (var mesh in model.GetComponentsInChildren<MeshRenderer>())
            {
                var material = mesh.material;
                var shader = material.shader;

                Material replacement;
                if (materials.TryGetValue(shader.name, out replacement))
                {
                    //Debug.Log("KVV: Looking for replacement for " + shader.name);
                    mesh.material.shader = replacement.shader;

                    // store current for later restoration
                    if (!meshLibrary.ContainsKey(mesh))
                    {
                        meshLibrary.Add(mesh, shader);
                    }
                }

#if DEBUG
                if (replacement == null)
                {
                    Debug.LogWarning("KVV: No replacement for " + shader.name);
                }
#endif
            }

            if (!_partShaderLibrary.ContainsKey(part))
            {
                _partShaderLibrary.Add(part, meshLibrary);
            }
        }

        private void RestorePartShaders(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;

            Dictionary<MeshRenderer, Shader> savedMats;
            if (!_partShaderLibrary.TryGetValue(part, out savedMats)) return;

            foreach (var mesh in model.GetComponentsInChildren<MeshRenderer>())
            {
                Shader oldShader;
                if (savedMats.TryGetValue(mesh, out oldShader))
                    mesh.material.shader = oldShader;
            }
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

        public Vector3 GetShipSize()
        {
            return CalcShipBounds().size;
        }

        public void UpdateCamera(Vector3 direction, int imageWidth, int imageHeight)
        {
            var minusDir = -direction;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            // todo: blueprint shader missing
            /*if (Effects["Blueprint"].Enabled)
                Camera.backgroundColor = new Color(1f, 1f, 1f, 0.0f);
            else*/
            Camera.backgroundColor = new Color(UiFloatVals["bgR"], UiFloatVals["bgG"], UiFloatVals["bgB"],
                UiFloatVals["bgA"]);

            Camera.transform.position = _shipBounds.center;

            // This sets the horizon before the camera looks to vehicle center.
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
                Camera.transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
            else
                Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.right);
            // this.Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.up); // original 

            // Apply angle Vector to camera.
            Camera.transform.Translate(minusDir*Camera.nearClipPlane);
            // this.Camera.transform.Translate(Vector3.Scale(minusDir, this.shipBounds.extents) + minusDir * this.Camera.nearClipPlane); // original 
            // Deckblad: There was a lot of math here when all we needed to do is establish the rotation of the camera.

            // Face camera to vehicle.
            Camera.transform.LookAt(_shipBounds.center);

            var tangent = Camera.transform.up;
            var binormal = Camera.transform.right;
            var height = Vector3.Scale(tangent, _shipBounds.size).magnitude;
            var width = Vector3.Scale(binormal, _shipBounds.size).magnitude;
            var depth = Vector3.Scale(minusDir, _shipBounds.size).magnitude;

            width += Config.procFairingOffset; // get the distance of fairing offset
            depth += Config.procFairingOffset; // for the farClipPlane

            // Find distance from vehicle.
            var positionOffset = (_shipBounds.size.magnitude - Position.z)/
                                 (2f*Mathf.Tan(Mathf.Deg2Rad*Camera.fieldOfView/2f));
            // float positionOffset = (height - this.position.z) / (2f * Mathf.Tan(Mathf.Deg2Rad * this.Camera.fieldOfView / 2f)) - depth * 0.5f; // original 
            // Use magnitude of bounds instead of height and remove vehicle bounds depth for uniform distance from vehicle. Height and depth of vehicle change in relation to the camera as we move around the vehicle.

            // Translate and Zoom camera
            Camera.transform.Translate(new Vector3(Position.x, Position.y, -positionOffset));

            // Get distance from camera to ship. Apply to farClipPlane
            var distanceToShip = Vector3.Distance(Camera.transform.position, _shipBounds.center);

            // Set far clip plane to just past size of vehicle.
            Camera.farClipPlane = distanceToShip + Camera.nearClipPlane + depth*2 + 1;
            // 1 for the first rotation vector
            // this.Camera.farClipPlane = Camera.nearClipPlane + positionOffset + this.position.magnitude + depth; // original

            if (Orthographic)
                Camera.orthographicSize = (Math.Max(height, width) - Position.z)/2f;

            var tmpAspect = width/height;
            if (height >= width)
            {
                CalculatedHeight = MaxHeight;
                CalculatedWidth = (int) (CalculatedHeight*tmpAspect);
            }
            else
            {
                CalculatedWidth = MaxWidth;
                CalculatedHeight = (int) (CalculatedWidth/tmpAspect);
            }

            // If we're saving, use full resolution.
            if ((imageWidth <= 0) || (imageHeight <= 0))
            {
                // Constrain image to max size with respect to aspect
                Camera.aspect = tmpAspect;
            }
            else
            {
                Camera.aspect = imageWidth/(float) imageHeight;
            }
        }

        public void GenTexture(Vector3 direction, int imageWidth = -1, int imageHeight = -1)
        {
            var isDrawing = UiBoolVals["canPreview"] || UiBoolVals["saveTextureEvent"];
            var isSaving = UiBoolVals["saveTextureEvent"];

            UpdateCamera(direction, imageWidth, imageHeight);

            if (_texture != null)
            {
                RenderTexture.ReleaseTemporary(_texture);
                _texture = null;
            }

            var fileWidth = imageWidth;
            var fileHeight = imageHeight;
            if (isSaving)
            {
                fileWidth =
                    (int) Math.Floor(imageWidth*(UiFloatVals["imgPercent"] >= 1 ? UiFloatVals["imgPercent"] : 1f));
                fileHeight =
                    (int) Math.Floor(imageHeight*(UiFloatVals["imgPercent"] >= 1 ? UiFloatVals["imgPercent"] : 1f));
            }

            if (isDrawing)
            {
                foreach (var p in EditorLogic.fetch.ship)
                    ReplacePartShaders(p);

                _texture = RenderTexture.GetTemporary(fileWidth, fileHeight, 24, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);

                Camera.targetTexture = _texture;
                Camera.depthTextureMode = DepthTextureMode.DepthNormals;
                Camera.Render();
                Camera.targetTexture = null;

                /*foreach (var fx in Effects)
                    if (fx.Value.Enabled)
                        Graphics.Blit(_texture, _texture, fx.Value.Material);*/

                foreach (var p in EditorLogic.fetch.ship)
                    RestorePartShaders(p);
            }

            Resources.UnloadUnusedAssets();
        }

        private void SaveTexture(string fileName)
        {
            var fileWidth = _texture.width;
            var fileHeight = _texture.height;
#if DEBUG
            Debug.Log(string.Format("KVV: SIZE: {0} x {1}", fileWidth, fileHeight));
#endif

            var screenShot = new Texture2D(fileWidth, fileHeight, TextureFormat.ARGB32, false);

            var saveRt = RenderTexture.active;
            RenderTexture.active = _texture;
            screenShot.ReadPixels(new Rect(0, 0, fileWidth, fileHeight), 0, 0);
            screenShot.Apply();
            RenderTexture.active = saveRt;
            var bytes = screenShot.EncodeToPNG();
            var shipNameFileSafe = MakeValidFileName(fileName);
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

            screenShot = null;
            bytes = null;
        }

        private static string MakeValidFileName(string name)
        {
            var invalidChars = Regex.Escape(new string(Path.GetInvalidFileNameChars()));
            var invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return Regex.Replace(name, invalidRegStr, "_");
        }

        public void Execute()
        {
            //if (!((EditorLogic.startPod) && (this.Ship != null)))
            if (!(EditorLogic.RootPart && (Ship != null)))
                return;

            SaveTexture("front_" + ShipName);
        }

        public void Explode()
        {
            //if (!EditorLogic.startPod || this.Ship == null)
            if (!EditorLogic.RootPart || (Ship == null))
                return;
            Config.Execute(Ship);
            UpdateShipBounds();
        }

        public void Update(int width = -1, int height = -1)
        {
            //if (!EditorLogic.startPod || this.Ship == null)
            if (!EditorLogic.RootPart || (Ship == null))
                return;

            //var dir = EditorLogic.startPod.transform.TransformDirection(this.direction);
            var dir = EditorLogic.RootPart.transform.TransformDirection(Direction);

            StoredShadowDistance = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = UiFloatVals["shadowVal"] < 0f ? 0f : UiFloatVals["shadowVal"];

            GenTexture(dir, width, height);

            QualitySettings.shadowDistance = StoredShadowDistance;
        }

        internal Texture Texture()
        {
            if (!(EditorLogic.RootPart && (Ship != null)))
                return null;
            return _texture;
        }
    }
}