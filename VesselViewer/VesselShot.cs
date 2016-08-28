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
        public ShaderMaterial MaterialFxaa = new ShaderMaterial("ShaderFXAA.txt");
        public ShaderMaterial MaterialColorAdjust = new ShaderMaterial("coloradjust");
        public ShaderMaterial MaterialEdgeDetect = new ShaderMaterial("edn2");
        public ShaderMaterial MaterialBluePrint = new ShaderMaterial("blueprint");

        private List<string> Shaders = new List<string>
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

        private Dictionary<string, Material> Materials;
        public readonly IDictionary<string, ShaderMaterial> Effects;
        public int calculatedWidth = 1;
        public int calculatedHeight = 1;

        public Dictionary<string, float> uiFloatVals = new Dictionary<string, float>
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

        public Dictionary<string, bool> uiBoolVals = new Dictionary<string, bool>
        {
            {"canPreview", true},
            {"saveTextureEvent", false}
        };

        private Camera[] cameras;
        private RenderTexture rt;
        private int maxWidth = 1024;
        private int maxHeight = 1024;
        private Bounds shipBounds;
        internal Camera Camera { get; private set; }
        internal Vector3 direction;
        internal Vector3 position;

        internal float storedShadowDistance;
            // keeps original shadow distance. Used to toggle shadows off during rendering.

        internal bool EffectsAntiAliasing { get; set; } //consider obsolete?

        internal bool Orthographic
        {
            get
            {
                return Camera == cameras[0];
                    //if this currently selected camera is the first camera then Orthographic is true
            }
            set
            {
                Camera = cameras[value ? 0 : 1];
                    //if setting to true use the first camera (which is ortho camera). if false use the non-ortho
            }
        }

        internal VesselViewConfig Config { get; private set; }

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
                {"FXAA", MaterialFxaa}
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

        ~VesselShot()
        {
            GameEvents.onPartAttach.Remove(PartModified);
            GameEvents.onPartRemove.Remove(PartModified);
        }

        // Sets up Orthographic and Perspective camera.
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

            if (HighLogic.LoadedScene == GameScenes.EDITOR)
            {
                Debug.Log(string.Format("Rotating in SPH: {0}", degrees));
                //rotateAxis = EditorLogic.startPod.transform.forward;
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
            foreach (var shaderPath in Shaders)
            {
                try
                {
                    var mat = new Material(KrsUtilsCore.AssetIndex.getShaderById(shaderPath));
                    Materials[mat.shader.name] = mat;
                }
                catch
                {
                    MonoBehaviour.print("[ERROR] " + GetType().Name + " : Failed to load " + shaderPath);
                }
            }
        }

        private void ReplacePartShaders(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;

            Dictionary<MeshRenderer, Shader> MeshRendererLibrary = new Dictionary<MeshRenderer, Shader>();

            foreach (MeshRenderer mr in model.GetComponentsInChildren<MeshRenderer>())
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

        Dictionary<Part, Dictionary<MeshRenderer, Shader>> PartShaderLibrary =
            new Dictionary<Part, Dictionary<MeshRenderer, Shader>>();

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
            if (uiBoolVals["canPreview"] || uiBoolVals["saveTextureEvent"])
            {
                foreach (Part p in EditorLogic.fetch.ship)
                {
                    ReplacePartShaders(p);
                }
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

            // This sets the horizon before the camera looks to vehicle center.
            if (HighLogic.LoadedScene == GameScenes.FLIGHT)
            {
                Camera.transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
            }
            else
            {
                Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.right);
            }
            // this.Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.up); // original 

            // Apply angle Vector to camera.
            Camera.transform.Translate(minusDir*Camera.nearClipPlane);
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
            float positionOffset = (shipBounds.size.magnitude - position.z)/
                                   (2f*Mathf.Tan(Mathf.Deg2Rad*Camera.fieldOfView/2f));
            // float positionOffset = (height - this.position.z) / (2f * Mathf.Tan(Mathf.Deg2Rad * this.Camera.fieldOfView / 2f)) - depth * 0.5f; // original 
            // Use magnitude of bounds instead of height and remove vehicle bounds depth for uniform distance from vehicle. Height and depth of vehicle change in relation to the camera as we move around the vehicle.

            // Translate and Zoom camera
            Camera.transform.Translate(new Vector3(position.x, position.y, -positionOffset));

            // Get distance from camera to ship. Apply to farClipPlane
            float distanceToShip = Vector3.Distance(Camera.transform.position, shipBounds.center);

            // Set far clip plane to just past size of vehicle.
            Camera.farClipPlane = distanceToShip + Camera.nearClipPlane + depth*2 + 1;
                // 1 for the first rotation vector
            // this.Camera.farClipPlane = Camera.nearClipPlane + positionOffset + this.position.magnitude + depth; // original

            if (Orthographic)
            {
                Camera.orthographicSize = (Math.Max(height, width) - position.z)/2f;
                    // Use larger of ship height or width.
                // this.Camera.orthographicSize = (height - this.position.z) / 2f; // original
            }

            bool isSaving = false;
            float tmpAspect = width/height;
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

            // If we're saving, use full resolution.
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

            int fileWidth = imageWidth;
            int fileHeight = imageHeight;
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
                //Graphics.Blit(this.rt, this.rt, MaterialColorAdjust.Material);
                //Graphics.Blit(this.rt, this.rt, MaterialEdgeDetect.Material);
                foreach (var fx in Effects)
                {
                    if (fx.Value.Enabled)
                    {
                        Graphics.Blit(rt, rt, fx.Value.Material);
                    }
                }
            }
            if (uiBoolVals["canPreview"] || uiBoolVals["saveTextureEvent"])
            {
                foreach (Part p in EditorLogic.fetch.ship)
                {
                    RestorePartShaders(p);
                }
            }
            if (uiBoolVals["saveTextureEvent"])
            {
                Resources.UnloadUnusedAssets(); //fix memory leak?
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

            //var dir = EditorLogic.startPod.transform.TransformDirection(this.direction);
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