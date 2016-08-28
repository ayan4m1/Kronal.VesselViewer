using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using File = KSP.IO.File;

namespace VesselViewer
{
    internal class VesselShot
    {
        public readonly IDictionary<string, ShaderMaterial> Effects;
        public int calculatedHeight = 1;
        public int calculatedWidth = 1;

        private Camera[] cameras;
        internal Vector3 direction;
        public string editorOrientation = "";
        public ShaderMaterial MaterialBluePrint = new ShaderMaterial(File.ReadAllText<VesselShot>("blueprint"));
        public ShaderMaterial MaterialColorAdjust = new ShaderMaterial(File.ReadAllText<VesselShot>("coloradjust"));
        public ShaderMaterial MaterialEdgeDetect = new ShaderMaterial(File.ReadAllText<VesselShot>("edn2"));
        public ShaderMaterial MaterialFXAA = new ShaderMaterial("FXAA");
        private readonly Dictionary<string, Material> Materials;
        private readonly int maxHeight = 1024;
        private readonly int maxWidth = 1024;

        private readonly Dictionary<Part, Dictionary<MeshRenderer, Shader>> PartShaderLibrary =
            new Dictionary<Part, Dictionary<MeshRenderer, Shader>>();

        internal Vector3 position;
        private RenderTexture rt;

        private readonly List<string> Shaders = new List<string>
        {
            "edn",
            "cutoff",
            "diffuse",
            "bumped",
            "bumpedspecular",
            "specular",
            "unlit",
            "emissivespecular",
            "emissivebumpedspecular"
        };

        private Bounds shipBounds;
        internal float storedShadowDistance;

        public Dictionary<string, bool> uiBoolVals = new Dictionary<string, bool>
        {
            {"canPreview", true},
            {"saveTextureEvent", false}
        };

        public Dictionary<string, float> uiFloatVals = new Dictionary<string, float>
        {
            {"shadowVal", 0f},
            {"shadowValPercent", 0f},
            {"imgPercent", 4f},
            {"bgR", 1f},
            {"bgG", 1f},
            {"bgB", 1f},
            {"bgA", 1f},
            {"bgR_", 0f},
            {"bgG_", 0.07f},
            {"bgB_", 0.11f},
            {"bgA_", 1f}
        };

        public VesselShot()
        {
            SetupCameras();
            Config = new VesselViewConfig();
            direction = Vector3.forward;
            Materials = new Dictionary<string, Material>();
            Effects = new Dictionary<string, ShaderMaterial>
            {
                {"Color Adjust", MaterialColorAdjust},
                {"Edge Detect", MaterialEdgeDetect},
                {"Blue Print", MaterialBluePrint},
                {"FXAA", MaterialFXAA}
            };
            Effects["Blue Print"].Enabled = false;
            uiFloatVals["bgR"] = uiFloatVals["bgR_"];
            uiFloatVals["bgG"] = uiFloatVals["bgG_"];
            uiFloatVals["bgB"] = uiFloatVals["bgB_"];
            LoadShaders();
            UpdateShipBounds();


            GameEvents.onPartAttach.Add(PartModified);
            GameEvents.onPartRemove.Add(PartModified);
        }

        internal Camera Camera { get; private set; }
        internal bool EffectsAntiAliasing { get; set; }

        internal bool Orthographic
        {
            get { return Camera == cameras[0]; }
            set { Camera = cameras[value ? 0 : 1]; }
        }

        internal VesselViewConfig Config { get; }

        internal IShipconstruct Ship
        {
            get
            {
                if (EditorLogic.fetch)
                {
                    return EditorLogic.fetch.ship;
                }
                return null;
            }
        }

        internal string ShipName
        {
            get
            {
                if (EditorLogic.fetch && EditorLogic.fetch.ship != null)
                {
                    return MakeValidFileName(EditorLogic.fetch.ship.shipName);
                }
                return "vessel";
            }
        }

        ~VesselShot()
        {
            GameEvents.onPartAttach.Remove(PartModified);
            GameEvents.onPartRemove.Remove(PartModified);
        }

        public void setFacility()
        {
            editorOrientation = EditorLogic.fetch.ship.shipFacility == EditorFacility.SPH ? "SPH" : "VAB";
        }

        private void SetupCameras()
        {
            cameras = new Camera[2];
            cameras[0] = new GameObject().AddComponent<Camera>();
            cameras[0].enabled = false;
            cameras[0].orthographic = true;
            cameras[0].cullingMask = EditorLogic.fetch.editorCamera.cullingMask & ~(1 << 16); /// hides kerbals
            cameras[0].transparencySortMode = TransparencySortMode.Orthographic;
            cameras[1] = new GameObject().AddComponent<Camera>();
            cameras[1].enabled = false;
            cameras[1].orthographic = false;
            cameras[1].cullingMask = cameras[0].cullingMask;
            Camera = cameras[0];
        }

        // Different rotations for SPH and VAB
        public void RotateShip(float degrees)
        {
            Vector3 rotateAxis;
            if (editorOrientation != "SPH" && editorOrientation != "VAB")
            {
                setFacility();
            }

            if (editorOrientation == "SPH")
            {
                Debug.Log(string.Format("Rotating in SPH: {0}", degrees));
                rotateAxis = EditorLogic.RootPart.transform.forward;
            }
            else
            {
                Debug.Log(string.Format("Rotating in VAB: {0}", degrees));
                rotateAxis = EditorLogic.RootPart.transform.up;
            }

            direction = Quaternion.AngleAxis(degrees, rotateAxis)*direction;
        }

        private void LoadShaders()
        {
            foreach (var shaderFilename in Shaders)
            {
                try
                {
                    var mat = new Material(Shader.Find(shaderFilename));
                    Materials[mat.shader.name] = mat;
                }
                catch
                {
                    MonoBehaviour.print("[ERROR] " + GetType().Name + " : Failed to load " + shaderFilename);
                }
            }
        }

        private void ReplacePartShaders(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;

            var MeshRendererLibrary = new Dictionary<MeshRenderer, Shader>();

            foreach (var mr in model.GetComponentsInChildren<MeshRenderer>())
            {
                Material mat;
                if (Materials.TryGetValue(mr.material.shader.name, out mat))
                {
                    if (!MeshRendererLibrary.ContainsKey(mr))
                    {
                        MeshRendererLibrary.Add(mr, mr.material.shader);
                    }
                    mr.material.shader = mat.shader;
                }
                else
                {
                    MonoBehaviour.print("[Warning] " + GetType().Name + "No replacement for " + mr.material.shader +
                                        " in " + part + "/*/" + mr);
                }
            }
            if (!PartShaderLibrary.ContainsKey(part))
            {
                PartShaderLibrary.Add(part, MeshRendererLibrary);
            }
        }

        private void RestorePartShaders(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;

            Dictionary<MeshRenderer, Shader> MeshRendererLibrary;
            if (PartShaderLibrary.TryGetValue(part, out MeshRendererLibrary))
            {
                foreach (var mr in model.GetComponentsInChildren<MeshRenderer>())
                {
                    Shader OldShader;
                    if (MeshRendererLibrary.TryGetValue(mr, out OldShader))
                    {
                        mr.material.shader = OldShader;
                    }
                }
            }
        }

        private void PartModified(GameEvents.HostTargetAction<Part, Part> data)
        {
            UpdateShipBounds();
        }

        internal void UpdateShipBounds()
        {
            if ((Ship != null) && (Ship.Parts.Count > 0))
            {
                shipBounds = CalcShipBounds();
            }
            else
            {
                shipBounds = new Bounds(EditorLogic.fetch.editorBounds.center, Vector3.zero);
            }
            shipBounds.Expand(1f);
        }

        private Bounds CalcShipBounds()
        {
            var result = new Bounds(Ship.Parts[0].transform.position, Vector3.zero);
            foreach (var current in Ship.Parts)
            {
                if (current.collider && !current.Modules.Contains("LaunchClamp"))
                {
                    result.Encapsulate(current.collider.bounds);
                }
            }
            return result;
        }

        public Vector3 GetShipSize()
        {
            return CalcShipBounds().size;
        }

        public void GenTexture(Vector3 direction, int imageWidth = -1, int imageHeight = -1)
        {
            foreach (Part p in EditorLogic.fetch.ship)
            {
                ReplacePartShaders(p);
            }

            var minusDir = -direction;
            Camera.clearFlags = CameraClearFlags.SolidColor;
            if (Effects["Blue Print"].Enabled)
            {
                Camera.backgroundColor = new Color(1f, 1f, 1f, 0.0f);
            }
            else
            {
                Camera.backgroundColor = new Color(uiFloatVals["bgR"], uiFloatVals["bgG"], uiFloatVals["bgB"],
                    uiFloatVals["bgA"]);
            }

            Camera.transform.position = shipBounds.center;

            //if (HighLogic.LoadedScene == GameScenes.SPH)
            if (editorOrientation == "SPH")
            {
                Camera.transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
            }
            else
            {
                Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.right);
            }

            Camera.transform.Translate(minusDir*Camera.nearClipPlane);

            // Face camera to vehicle.
            Camera.transform.LookAt(shipBounds.center);

            var tangent = Camera.transform.up;
            var binormal = Camera.transform.right;
            var height = Vector3.Scale(tangent, shipBounds.size).magnitude;
            var width = Vector3.Scale(binormal, shipBounds.size).magnitude;
            var depth = Vector3.Scale(minusDir, shipBounds.size).magnitude;

            width += Config.procFairingOffset;
            depth += Config.procFairingOffset;

            var positionOffset = (shipBounds.size.magnitude - position.z)/
                                 (2f*Mathf.Tan(Mathf.Deg2Rad*Camera.fieldOfView/2f));

            Camera.transform.Translate(new Vector3(position.x, position.y, -positionOffset));
            var distanceToShip = Vector3.Distance(Camera.transform.position, shipBounds.center);
            Camera.farClipPlane = distanceToShip + Camera.nearClipPlane + depth*2 + 1;
                // 1 for the first rotation vector

            if (Orthographic)
            {
                Camera.orthographicSize = (Math.Max(height, width) - position.z)/2f;
                    // Use larger of ship height or width.
            }

            var isSaving = false;
            var tmpAspect = width/height;
            if (height >= width)
            {
                calculatedHeight = maxHeight;
                calculatedWidth = (int) (calculatedHeight*tmpAspect);
            }
            else
            {
                calculatedWidth = maxWidth;
                calculatedHeight = (int) (calculatedWidth/tmpAspect);
            }

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                // Constrain image to max size with respect to aspect
                isSaving = true;
                Camera.aspect = tmpAspect;
                imageWidth = calculatedWidth;
                imageHeight = calculatedHeight;
            }
            else
            {
                Camera.aspect = imageWidth/(float) imageHeight;
            }
            if (rt) RenderTexture.ReleaseTemporary(rt);
            rt = RenderTexture.GetTemporary(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB);

            var fileWidth = imageWidth;
            var fileHeight = imageHeight;
            if (isSaving)
            {
                fileWidth =
                    (int) Math.Floor(imageWidth*(uiFloatVals["imgPercent"] >= 1 ? uiFloatVals["imgPercent"] : 1f));
                fileHeight =
                    (int) Math.Floor(imageHeight*(uiFloatVals["imgPercent"] >= 1 ? uiFloatVals["imgPercent"] : 1f));
            }

            if (uiBoolVals["canPreview"] || uiBoolVals["saveTextureEvent"])
            {
                rt = RenderTexture.GetTemporary(fileWidth, fileHeight, 24, RenderTextureFormat.ARGB32,
                    RenderTextureReadWrite.sRGB);
                Camera.targetTexture = rt;
                Camera.depthTextureMode = DepthTextureMode.DepthNormals;
                Camera.Render();
                Camera.targetTexture = null;
                foreach (var fx in Effects)
                {
                    if (fx.Value.Enabled)
                    {
                        Graphics.Blit(rt, rt, fx.Value.Material);
                    }
                }
            }

            foreach (Part p in EditorLogic.fetch.ship)
            {
                RestorePartShaders(p);
            }
        }

        private void SaveTexture(string fileName)
        {
            var fileWidth = rt.width;
            var fileHeight = rt.height;
#if DEBUG
            Debug.Log(string.Format("KVV: SIZE: {0} x {1}", fileWidth, fileHeight));
#endif

            var screenShot = new Texture2D(fileWidth, fileHeight, TextureFormat.ARGB32, false);

            var saveRt = RenderTexture.active;
            RenderTexture.active = rt;
            screenShot.ReadPixels(new Rect(0, 0, fileWidth, fileHeight), 0, 0);
            screenShot.Apply();
            RenderTexture.active = saveRt;
            var bytes = screenShot.EncodeToPNG();
            var ShipNameFileSafe = MakeValidFileName(fileName);
            uint file_inc = 0;
            var filename = "";
            var filenamebase = "";

            do
            {
                ++file_inc;
                filenamebase = ShipNameFileSafe + "_" + file_inc + ".png";
                filename = Path.Combine(Directory.GetParent(KSPUtil.ApplicationRootPath).ToString(),
                    "Screenshots" + Path.DirectorySeparatorChar + filenamebase);
            } while (System.IO.File.Exists(filename));
            System.IO.File.WriteAllBytes(filename, bytes);

            Debug.Log(string.Format("KVV: Took screenshot to: {0}", filename));
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
            {
                return;
            }

            SaveTexture("front" + "_" + ShipName);
        }

        public void Explode()
        {
            //if (!EditorLogic.startPod || this.Ship == null)
            if (!EditorLogic.RootPart || Ship == null)
            {
                return;
            }
            Config.Execute(Ship);
            UpdateShipBounds();
        }

        public void Update(int width = -1, int height = -1)
        {
            //if (!EditorLogic.startPod || this.Ship == null)
            if (!EditorLogic.RootPart || Ship == null)
            {
                return;
            }

            var dir = EditorLogic.RootPart.transform.TransformDirection(direction);

            storedShadowDistance = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = uiFloatVals["shadowVal"] < 0f ? 0f : uiFloatVals["shadowVal"];

            GenTexture(dir, width, height);

            QualitySettings.shadowDistance = storedShadowDistance;
        }

        internal Texture Texture()
        {
            if (!(EditorLogic.RootPart && (Ship != null)))
            {
                return null;
            }
            return rt;
        }
    }
}