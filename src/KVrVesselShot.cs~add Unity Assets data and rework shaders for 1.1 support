using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPAssets;


namespace KronalUtils
{
    class KVrVesselShot
    {
        //KSPAssets.AssetDefinition[] KVrShaders = KSPAssets.Loaders.AssetLoader.GetAssetDefinitionsWithType(KronalUtils.Properties.Resources.ShaderFXAA, typeof(Shader));
        /*
        public Dictionary<string, string> KVrShaders = new Dictionary<string, string> {
            { "MaterialFXAA", KronalUtils.Properties.Resources.ShaderFXAA },
            { "MaterialColorAdjust", KSP.IO.File.ReadAllText<KVrVesselShot>("coloradjust") },
            { "MaterialEdgeDetect", KSP.IO.File.ReadAllText<KVrVesselShot>("edn2") },
            { "MaterialBluePrint", KSP.IO.File.ReadAllText<KVrVesselShot>("blueprint") },
        };*/
        //public KVrUtilsCore KVVCore = new KVrUtilsCore();
        public Dictionary<string, string> KVrShaders = new Dictionary<string, string> {
            { "MaterialFXAA", "KVV/Hidden/SlinDev/Desktop/PostProcessing/FXAA" },
            { "MaterialColorAdjust", "KVV/Color Adjust" },
            { "MaterialEdgeDetect", "KVV/Hidden/Edge Detect Normals2" },
        };
        public ShaderMaterial MaterialFXAA = new ShaderMaterial(KronalUtils.Properties.Resources.ShaderFXAA);
        public ShaderMaterial MaterialColorAdjust = new ShaderMaterial(KSP.IO.File.ReadAllText<KVrVesselShot>("coloradjust"));
        public ShaderMaterial MaterialEdgeDetect = new ShaderMaterial(KSP.IO.File.ReadAllText<KVrVesselShot>("edn2"));
        //public ShaderMaterial MaterialBluePrint = new ShaderMaterial(KSP.IO.File.ReadAllText<KVrVesselShot>("blueprint"));/**/
        private List<string> Shaders = new List<string>() { "edn", "cutoff", "diffuse", "bumped", "bumpedspecular", "specular", "unlit", "emissivespecular", "emissivebumpedspecular" };
        private Dictionary<string, Material> Materials;
        public string editorOrientation = "";//SPH|VAB
        public readonly IDictionary<string, ShaderMaterial> Effects;
        public int calculatedWidth = 1;
        public int calculatedHeight = 1;
        public Dictionary<string, float> uiFloatVals = new Dictionary<string, float> { 
            { "shadowVal", 0f }, { "shadowValPercent", 0f },
            {"imgPercent",4f},
            {"bgR",1f},{"bgG",1f},{"bgB",1f},{"bgA",1f},//RGBA
            {"bgR_",0f},{"bgG_",0.07f},{"bgB_",0.11f},{"bgA_",1f}//RGBA defaults //00406E 0,64,110 -> reduced due to color adjust shader
        };
        public Dictionary<string, bool> uiBoolVals = new Dictionary<string, bool> {
            {"canPreview",true},{"saveTextureEvent",false}
        };
        
        private Camera[] cameras;
        private RenderTexture rt;
        private int maxWidth = 1024;
        private int maxHeight = 1024;
        private Bounds shipBounds;
        internal Camera Camera { get; private set; }
        internal Vector3 direction;
        internal Vector3 position;
        internal float storedShadowDistance; // keeps original shadow distance. Used to toggle shadows off during rendering.
        internal bool EffectsAntiAliasing { get; set; }//consider obsolete?
        internal bool Orthographic
        {
            get
            {
                return this.Camera == this.cameras[0];//if this currently selected camera is the first camera then Orthographic is true
            }
            set
            {
                this.Camera = this.cameras[value ? 0 : 1];//if setting to true use the first camera (which is ortho camera). if false use the non-ortho
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
                else
                {
                    return null;
                }
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
                else
                {
                    return "vessel";
                }
            }
        }

        public KVrVesselShot()
        {
            SetupCameras();
            this.Config = new VesselViewConfig();
            this.direction = Vector3.forward;
            this.Materials = new Dictionary<string, Material>();
            this.Effects = new Dictionary<string, ShaderMaterial>() {
                {"Color Adjust",MaterialColorAdjust},
                {"Edge Detect", MaterialEdgeDetect},
                //{"Blue Print", MaterialBluePrint},
                {"FXAA", MaterialFXAA}
            };
            //this.Effects["Blue Print"].Enabled = false;
            uiFloatVals["bgR"]=uiFloatVals["bgR_"];
            uiFloatVals["bgG"]=uiFloatVals["bgG_"];
            uiFloatVals["bgB"]=uiFloatVals["bgB_"];
            LoadShaders();
            UpdateShipBounds();
            

            GameEvents.onPartAttach.Add(PartModified);
            GameEvents.onPartRemove.Add(PartModified);
        }

        ~KVrVesselShot()
        {
            GameEvents.onPartAttach.Remove(PartModified);
            GameEvents.onPartRemove.Remove(PartModified);
        }
        public void setFacility()
        {   
            editorOrientation = (EditorLogic.fetch.ship.shipFacility == EditorFacility.SPH ? "SPH" : "VAB");
        }
        private void SetupCameras()
        {
            this.cameras = new Camera[2];
            this.cameras[0] = new GameObject().AddComponent<Camera>();
            this.cameras[0].enabled = false;
            this.cameras[0].orthographic = true;
            this.cameras[0].cullingMask = EditorLogic.fetch.editorCamera.cullingMask & ~(1 << 16); /// hides kerbals
            this.cameras[0].transparencySortMode = TransparencySortMode.Orthographic;
            this.cameras[1] = new GameObject().AddComponent<Camera>();
            this.cameras[1].enabled = false;
            this.cameras[1].orthographic = false;
            this.cameras[1].cullingMask = this.cameras[0].cullingMask;
            this.Camera = this.cameras[0];
        }

        // Different rotations for SPH and VAB
        public void RotateShip(float degrees)
        {
            Vector3 rotateAxis;
            if (editorOrientation != "SPH" && editorOrientation != "VAB") { setFacility(); }

            //if (HighLogic.LoadedScene == GameScenes.SPH)
            if (editorOrientation == "SPH")
            {
#if DEBUG
                Debug.Log(string.Format("Rotating in SPH: {0}", degrees));
#endif
                //rotateAxis = EditorLogic.startPod.transform.forward;
                rotateAxis = EditorLogic.RootPart.transform.forward;
            }
            else
            {
#if DEBUG
                Debug.Log(string.Format("Rotating in VAB: {0}", degrees));
#endif
                //rotateAxis = EditorLogic.startPod.transform.up;
                rotateAxis = EditorLogic.RootPart.transform.up;
            }

            this.direction = Quaternion.AngleAxis(degrees, rotateAxis) * this.direction;
        }        

        private void LoadShaders()
        {
            foreach (var shaderFilename in Shaders)
            {
                try
                {
                    var mat = new Material(KVrUtils.GetResourceString(shaderFilename));
                    Materials[mat.shader.name] = mat;
                }
                catch
                {
                    //MonoBehaviour.print("[ERROR] " + this.GetType().Name + " : Failed to load " + shaderFilename);
#if DEBUG
                    Debug.Log(string.Format("KVV: LoadShaders [ERROR] " + this.GetType().Name + " : Failed to load " + shaderFilename));
#endif
                }
            }
        }

        private void ReplacePartShaders(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;

            Dictionary<MeshRenderer, Shader> MeshRendererLibrary = new Dictionary<MeshRenderer,Shader>();

            foreach (MeshRenderer mr in model.GetComponentsInChildren<MeshRenderer>())
            {
                Material mat;
                if (Materials.TryGetValue(mr.material.shader.name, out mat))
                {
                    if (!MeshRendererLibrary.ContainsKey(mr)) {
                        MeshRendererLibrary.Add(mr, mr.material.shader);
                    }
                    mr.material.shader = mat.shader;
                } else {
                    //MonoBehaviour.print("[Warning] " + this.GetType().Name + "No replacement for " + mr.material.shader + " in " + part + "/*/" + mr);
#if DEBUG
                    Debug.Log(string.Format("KVV: LoadShaders [Warning] " + this.GetType().Name + "No replacement for " + mr.material.shader + " in " + part + "/*/" + mr));
#endif
                }
            }
            if (!PartShaderLibrary.ContainsKey(part))
            {
                PartShaderLibrary.Add(part, MeshRendererLibrary);
            }
        }
        
        Dictionary<Part,Dictionary<MeshRenderer, Shader>> PartShaderLibrary = new Dictionary<Part,Dictionary<MeshRenderer,Shader>>();

        private void RestorePartShaders(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;
            
            Dictionary<MeshRenderer,Shader> MeshRendererLibrary;
            if (PartShaderLibrary.TryGetValue(part, out MeshRendererLibrary))
            {
                foreach (MeshRenderer mr in model.GetComponentsInChildren<MeshRenderer>())
                {
                    Shader OldShader;
                    if (MeshRendererLibrary.TryGetValue(mr, out OldShader))
                    {
                        mr.material.shader = OldShader;
                    }
                }
            }            
        }
        
        //private void PartModified(Part data)
        private void PartModified(GameEvents.HostTargetAction<Part, Part> data)
        {
            UpdateShipBounds();
        }

        internal void UpdateShipBounds()
        {
            if ((this.Ship != null) && (this.Ship.Parts.Count > 0))
            {
                this.shipBounds = CalcShipBounds();
            }
            else
            {
                this.shipBounds = new Bounds(EditorLogic.fetch.editorBounds.center, Vector3.zero);
            }
            this.shipBounds.Expand(1f);
        }

        private Bounds CalcShipBounds()
        {
            Bounds result = new Bounds(this.Ship.Parts[0].transform.position, Vector3.zero);
            foreach (var current in this.Ship.Parts)
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
            this.Camera.clearFlags = CameraClearFlags.SolidColor;
            this.Camera.backgroundColor = new Color(uiFloatVals["bgR"], uiFloatVals["bgG"], uiFloatVals["bgB"], uiFloatVals["bgA"]);
            this.Camera.transform.position = this.shipBounds.center;

            //if (HighLogic.LoadedScene == GameScenes.SPH)
            if (editorOrientation == "SPH")
            {
                this.Camera.transform.rotation = Quaternion.AngleAxis(90, Vector3.right);
            }
            else
            {
                this.Camera.transform.rotation = Quaternion.AngleAxis(0f, Vector3.right);
            }

            // Apply angle Vector to camera.
            this.Camera.transform.Translate(minusDir * this.Camera.nearClipPlane);

            // Face camera to vehicle.
            this.Camera.transform.LookAt(this.shipBounds.center);

            var tangent = this.Camera.transform.up;
            var binormal = this.Camera.transform.right;
            var height = Vector3.Scale(tangent, this.shipBounds.size).magnitude;
            var width = Vector3.Scale(binormal, this.shipBounds.size).magnitude;
            var depth = Vector3.Scale(minusDir, this.shipBounds.size).magnitude;

            width += this.Config.procFairingOffset; // get the distance of fairing offset
            depth += this.Config.procFairingOffset; // for the farClipPlane

            // Find distance from vehicle.
            float positionOffset = (this.shipBounds.size.magnitude - this.position.z) / (2f * Mathf.Tan(Mathf.Deg2Rad * this.Camera.fieldOfView / 2f));

            this.Camera.transform.Translate(new Vector3(this.position.x, this.position.y, -positionOffset));
            float distanceToShip = Vector3.Distance(this.Camera.transform.position, this.shipBounds.center);
            this.Camera.farClipPlane = distanceToShip + this.Camera.nearClipPlane + depth * 2 + 1; // 1 for the first rotation vector
            
            if (this.Orthographic)
            {
                this.Camera.orthographicSize = (Math.Max(height, width) - this.position.z) / 2f; // Use larger of ship height or width.
            }

            bool isSaving = false;
            float tmpAspect = width / height;
            if (height >= width)
            {
                this.calculatedHeight = (int)maxHeight;
                this.calculatedWidth = (int)(this.calculatedHeight * tmpAspect);
            }
            else
            {
                this.calculatedWidth = (int)maxWidth;
                this.calculatedHeight = (int)(this.calculatedWidth / tmpAspect);
            }

            if (imageWidth <= 0 || imageHeight <= 0)
            {
                // Constrain image to max size with respect to aspect
                isSaving = true;
                this.Camera.aspect = tmpAspect;
                imageWidth = this.calculatedWidth;
                imageHeight = this.calculatedHeight;
            }
            else
            {
                this.Camera.aspect = (float) imageWidth / (float) imageHeight;
            }
            if (this.rt) RenderTexture.ReleaseTemporary(this.rt);
            this.rt = RenderTexture.GetTemporary(imageWidth, imageHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);

            int fileWidth = imageWidth;
            int fileHeight = imageHeight;
            if (isSaving)
            {
                fileWidth = (int)Math.Floor(imageWidth * (uiFloatVals["imgPercent"] >= 1 ? uiFloatVals["imgPercent"] : 1f));
                fileHeight = (int)Math.Floor(imageHeight * (uiFloatVals["imgPercent"] >= 1 ? uiFloatVals["imgPercent"] : 1f));
            }

            if (uiBoolVals["canPreview"] || uiBoolVals["saveTextureEvent"])
            {
                this.rt = RenderTexture.GetTemporary(fileWidth, fileHeight, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
                this.Camera.targetTexture = this.rt;
                this.Camera.depthTextureMode = DepthTextureMode.DepthNormals;
                this.Camera.Render();
                this.Camera.targetTexture = null;
                //Graphics.Blit(this.rt, this.rt, MaterialColorAdjust.Material);
                //Graphics.Blit(this.rt, this.rt, MaterialEdgeDetect.Material);
                foreach (var fx in Effects)
                {
                    if (fx.Value.Enabled)
                    {
                        Graphics.Blit(this.rt, this.rt, fx.Value.Material);
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
                Resources.UnloadUnusedAssets();//fix memory leak?
            }
        }

        private void SaveTexture(String fileName)
        {
            int fileWidth = this.rt.width;
            int fileHeight = this.rt.height;
#if DEBUG
            Debug.Log(string.Format("KVV: SIZE: {0} x {1}", fileWidth, fileHeight));
#endif

            Texture2D screenShot = new Texture2D(fileWidth, fileHeight, TextureFormat.ARGB32, false);
            
            var saveRt = RenderTexture.active;
            RenderTexture.active = this.rt;
            screenShot.ReadPixels(new Rect(0, 0, fileWidth, fileHeight), 0, 0);
            screenShot.Apply();
            RenderTexture.active = saveRt;
            byte[] bytes = screenShot.EncodeToPNG();
			string ShipNameFileSafe = MakeValidFileName(fileName);
            uint file_inc = 0;
            string filename = "";
            string filenamebase = "";
            
            do{
                ++file_inc;
                filenamebase = ShipNameFileSafe + "_" + file_inc.ToString() + ".png";
                //filename = Path.Combine(System.IO.Directory.GetParent(KSPUtil.ApplicationRootPath).ToString(), "Screenshots" + Path.DirectorySeparatorChar + filenamebase);
                filename = Path.Combine(KVrUtilsCore.ModExport() + Path.DirectorySeparatorChar, filenamebase);
            } while(File.Exists(filename));
            System.IO.File.WriteAllBytes(filename, bytes);

            Debug.Log(string.Format("KVV: Took screenshot to: {0}", filename));
            screenShot = null;
            bytes = null;
        }
        private static string MakeValidFileName(string name)
        {
            string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
            string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);
            return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
        }
        public void Execute() {
            //if (!((EditorLogic.startPod) && (this.Ship != null)))
            if (!((EditorLogic.RootPart) && (this.Ship != null)))
            {
                return;
            }

            SaveTexture("front" + "_" + ShipName);
        }

        public void Explode()
        {
            //if (!EditorLogic.startPod || this.Ship == null)
            if (!EditorLogic.RootPart || this.Ship == null)
            {
                return;
            }
            this.Config.Execute(this.Ship);
            UpdateShipBounds();

        }

        public void Update(int width = -1, int height = -1)
        {
            //if (!EditorLogic.startPod || this.Ship == null)
            if (!EditorLogic.RootPart || this.Ship == null)
            {
                return;
            }

            //var dir = EditorLogic.startPod.transform.TransformDirection(this.direction);
            var dir = EditorLogic.RootPart.transform.TransformDirection(this.direction);
            
            storedShadowDistance = QualitySettings.shadowDistance;
            QualitySettings.shadowDistance = (this.uiFloatVals["shadowVal"] < 0f ? 0f : this.uiFloatVals["shadowVal"]);
            
            GenTexture(dir, width, height);

            QualitySettings.shadowDistance = storedShadowDistance;
            
        }

        internal Texture Texture()//not used?!
        {
            
            //if (!((EditorLogic.startPod) && (this.Ship != null)))
            if (!((EditorLogic.RootPart) && (this.Ship != null)))
            {
                return null;
            }
            else
            {
                return this.rt;
            }
        }
    }
}
