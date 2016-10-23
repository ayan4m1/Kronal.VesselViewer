using System;
using System.Linq;
using KSP.UI.Screens;
using UnityEngine;
using VesselViewer.Container;
using VesselViewer.Services;

namespace VesselViewer
{
    [KSPAddon(KSPAddon.Startup.EditorAny, false)]
    public class VesselShotUi : MonoBehaviour
    {
        private AppContainer _application;

        private EditorAxis axis;
        private readonly VesselShot control;
        private GUIStyle guiStyleButtonAlert;
        private readonly string inputLockIdent = "KVV-EditorLock";
        private ApplicationLauncherButton KVVButton;
        private bool mySoftLock; //not to be confused with EditorLogic.softLock
        private Rect orthoViewRect;
        private int shaderTabCurrent;
        private string[] shaderTabsNames;
        private int tabCurrent; //almost obsolete
        private bool visible;
        private Vector2 windowScrollPos;
        private Rect windowSize;

        public VesselShotUi()
        {
            _application = new AppContainer();
        }

        private bool IsOnEditor()
        {
            return (HighLogic.LoadedScene == GameScenes.EDITOR) || HighLogic.LoadedSceneIsEditor;
        }

        public void Awake()
        {
            var effectService = _application.Container.Resolve<IEffectService>();
            //var assetService = _application.Container.Resolve

            Debug.Log("KVV: Starting load coroutine");
            StartCoroutine(KrsUtils.LoadBundle());

            windowSize = new Rect(256f, 50f, 300f, Screen.height - 50f);
            shaderTabsNames = effectService.GetNames().Concat(new[] {"Part Config"}).ToArray();

            control.Config.onApply += ConfigApplied;
            control.Config.onRevert += ConfigReverted;

            GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
        }

        private void Start()
        {
            if (KVVButton == null)
                OnGUIAppLauncherReady();
        }

        private void ConfigApplied()
        {
            ButtonMode(false);
        }

        private void ConfigReverted()
        {
            EditorLogic.fetch.Unlock(GetInstanceID().ToString());
            ButtonMode(true);
        }

        private void ButtonMode(bool isOn)
        {
            if (isOn)
            {
                EditorLogic.fetch.partPanelBtn.enabled = true;
                EditorLogic.fetch.actionPanelBtn.enabled = true;
                EditorLogic.fetch.crewPanelBtn.enabled = true;
                EditorLogic.fetch.saveBtn.enabled = true;
                EditorLogic.fetch.launchBtn.enabled = true;
                EditorLogic.fetch.exitBtn.enabled = true;
                EditorLogic.fetch.loadBtn.enabled = true;
                EditorLogic.fetch.newBtn.enabled = true;
            }
            else
            {
                EditorLogic.fetch.partPanelBtn.enabled = true;
                EditorLogic.fetch.actionPanelBtn.enabled = true;
                EditorLogic.fetch.crewPanelBtn.enabled = true;
                EditorLogic.fetch.saveBtn.enabled = false;
                EditorLogic.fetch.launchBtn.enabled = false;
                EditorLogic.fetch.exitBtn.enabled = false;
                EditorLogic.fetch.loadBtn.enabled = true;
                EditorLogic.fetch.newBtn.enabled = true;
            }
        }

        public void Update()
        {
            if ((tabCurrent == 0) && (orthoViewRect.width*orthoViewRect.height > 1f))
                control.Update((int) orthoViewRect.width*2, (int) orthoViewRect.height*2);
        }

        private bool isMouseOver() //https://github.com/m4v/RCSBuildAid/blob/master/Plugin/GUI/MainWindow.cs
        {
            var position = new Vector2(Input.mousePosition.x,
                Screen.height - Input.mousePosition.y);
            return windowSize.Contains(position);
        }

        /* Whenever we mouseover our window, we need to lock the editor so we don't pick up
         * parts while dragging the window around */

        private void setEditorLock() //https://github.com/m4v/RCSBuildAid/blob/master/Plugin/GUI/MainWindow.cs#L296
        {
            if (visible)
            {
                var mouseOver = isMouseOver();
                if (mouseOver && !mySoftLock)
                {
                    mySoftLock = true;
                    var controlTypes = ControlTypes.CAMERACONTROLS
                                       | ControlTypes.EDITOR_ICON_HOVER
                                       | ControlTypes.EDITOR_ICON_PICK
                                       | ControlTypes.EDITOR_PAD_PICK_PLACE
                                       | ControlTypes.EDITOR_PAD_PICK_COPY
                                       | ControlTypes.EDITOR_EDIT_STAGES
                                       | ControlTypes.EDITOR_GIZMO_TOOLS
                                       | ControlTypes.EDITOR_ROOT_REFLOW;

                    InputLockManager.SetControlLock(controlTypes, inputLockIdent);
                }
                else if (!mouseOver && mySoftLock)
                {
                    mySoftLock = false;
                    InputLockManager.RemoveControlLock(inputLockIdent);
                }
            }
            else if (mySoftLock)
            {
                mySoftLock = false;
                InputLockManager.RemoveControlLock(inputLockIdent);
            }
        }

        public void OnGUI()
        {
            switch (HighLogic.LoadedScene)
            {
//https://github.com/m4v/RCSBuildAid/blob/master/Plugin/GUI/MainWindow.cs
                case GameScenes.EDITOR:
                    //case GameScenes.SPH:
                    break;
                default:
                    /* don't show window during scene changes */
                    return;
            }
            if (visible)
                windowSize = GUILayout.Window(GetInstanceID(), windowSize, GUIWindow, "Kronal Vessel Viewer",
                    HighLogic.Skin.window);

            if (Event.current.type == EventType.Repaint)
                setEditorLock();
        }

        private void GUIWindow(int id)
        {
            GUILayout.BeginVertical("box");
            GUIButtons(); //draw top buttons
            GUITabShader(shaderTabsNames[shaderTabCurrent]); //draw shader control buttons
            GUITabView(); //show the screenshot preview
            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        private void GUIButtons()
        {
            if (guiStyleButtonAlert == null)
            {
                guiStyleButtonAlert = new GUIStyle(GUI.skin.button);
                guiStyleButtonAlert.active.textColor = XKCDColors.BrightRed;
                guiStyleButtonAlert.hover.textColor = XKCDColors.Red;
                guiStyleButtonAlert.normal.textColor = XKCDColors.DarkishRed;
                guiStyleButtonAlert.fontStyle = FontStyle.Bold;
                guiStyleButtonAlert.fontSize = 8;
                guiStyleButtonAlert.stretchWidth = false;
                guiStyleButtonAlert.alignment = TextAnchor.MiddleCenter;
            }
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("Toggle Offset"))
                control.Explode();

            if (GUILayout.Button("Revert"))
                control.Config.Revert();

            if (GUILayout.Button("Screenshot"))
            {
                control.Update();
                control.SaveTexture("front_");
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("ᴖ", GUILayout.Width(34), GUILayout.Height(34)))
                control.Direction = Quaternion.AngleAxis(-0.4f, control.Camera.transform.right)*control.Direction;
            if (GUILayout.RepeatButton("ϲ", GUILayout.Width(34), GUILayout.Height(34)))
                control.RotateShip(-1f);
            if (GUILayout.RepeatButton("▲", GUILayout.Width(34), GUILayout.Height(34)))
                control.Position.y -= 0.1f;
            if (GUILayout.RepeatButton("ᴐ", GUILayout.Width(34), GUILayout.Height(34)))
                control.RotateShip(1f);
            if (GUILayout.RepeatButton("+", GUILayout.Width(34), GUILayout.Height(34)))
                control.Position.z += 0.1f;
            if (GUILayout.Button("RESET", guiStyleButtonAlert, GUILayout.Width(34), GUILayout.Height(34)))
            {
                control.Direction = Vector3.forward;
                control.Position = Vector3.zero;
            }
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.RepeatButton("ᴗ", GUILayout.Width(34), GUILayout.Height(34)))
                control.Direction = Quaternion.AngleAxis(0.4f, control.Camera.transform.right)*control.Direction;
            if (GUILayout.RepeatButton("◄", GUILayout.Width(34), GUILayout.Height(34)))
                control.Position.x += 0.1f;
            if (GUILayout.RepeatButton("▼", GUILayout.Width(34), GUILayout.Height(34)))
                control.Position.y += 0.1f;
            if (GUILayout.RepeatButton("►", GUILayout.Width(34), GUILayout.Height(34)))
                control.Position.x -= 0.1f;
            if (GUILayout.RepeatButton("-", GUILayout.Width(34), GUILayout.Height(34)))
                control.Position.z -= 0.1f;
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();

            GUILayout.BeginVertical();
            control.Orthographic = GUILayout.Toggle(control.Orthographic, "Orthographic", GUILayout.ExpandWidth(true));
            GUILayout.BeginHorizontal();
            control.UiFloatVals["shadowVal"] = 0f;
            GUILayout.Label("Shadow", GUILayout.Width(46f));
            GUILayout.Space(3f);
            control.UiFloatVals["shadowValPercent"] = GUILayout.HorizontalSlider(
                control.UiFloatVals["shadowValPercent"], 0f, 300f, GUILayout.Width(153f));
            GUILayout.Space(1f);
            GUILayout.Label(control.UiFloatVals["shadowValPercent"].ToString("F"), GUILayout.Width(50f));
            //GUILayout.Width(50f),
            control.UiFloatVals["shadowVal"] = control.UiFloatVals["shadowValPercent"] * 1000f;
            //1000 is the max shadow val.  Looks like it takes a float so thats the max? 
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            GUILayout.Label("File Quality", GUILayout.Width(68f));
            GUILayout.Space(1f);
            control.UiFloatVals["imgPercent"] = GUILayout.HorizontalSlider(control.UiFloatVals["imgPercent"] - 1, 0f, 8f,
                GUILayout.Width(140f)) + 1f;
            GUILayout.Space(1f);
            var disW = Math.Floor(control.UiFloatVals["imgPercent"] * control.CalculatedWidth);
            var disH = Math.Floor(control.UiFloatVals["imgPercent"] * control.CalculatedHeight);
            GUILayout.Label($"{control.UiFloatVals["imgPercent"]:0.#}\n{disW} x {disH}", GUILayout.Width(110f));
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            shaderTabCurrent = GUILayout.Toolbar(shaderTabCurrent, shaderTabsNames);
            GUILayout.EndHorizontal();

            tabCurrent = 0; //used only in Update() be 0.  This will be removed later
        }

        private void GUITabShader(string name)
        {
            if (!control.Effects.ContainsKey(name)) // effect not found!
            {
                GUILayout.BeginHorizontal();
                GUITabConfig();
                GUILayout.EndHorizontal();
                return;
            }
            GUILayout.BeginHorizontal();
            control.Effects[name].Enabled = GUILayout.Toggle(control.Effects[name].Enabled, "Active");
            GUILayout.EndHorizontal();
            for (var i = 0; i < control.Effects[name].PropertyCount; ++i)
            {
                var prop = control.Effects[name][i];
                prop.Match(
                    p =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        p.Value = GUILayout.HorizontalSlider(p.Value, p.RangeMin, p.RangeMax);
                        GUILayout.Label(p.Value.ToString("F"), GUILayout.Width(30f));
                        if (GUILayout.Button("RESET", guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    },
                    IfColor: p =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        GUILayout.BeginVertical();
                        Color oldVal = p.Value, newVal;
                        newVal.r = GUILayout.HorizontalSlider(oldVal.r, 0f, 1f);
                        newVal.g = GUILayout.HorizontalSlider(oldVal.g, 0f, 1f);
                        newVal.b = GUILayout.HorizontalSlider(oldVal.b, 0f, 1f);
                        newVal.a = 1f;
                        if (newVal != oldVal) p.Value = newVal;
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        GUILayout.Label(oldVal.r.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.g.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.b.ToString("F"), GUILayout.Width(40f));
                        GUILayout.EndVertical();
                        if (GUILayout.Button("RESET", guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    },
                    IfVector: p =>
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Label(p.DisplayName, GUILayout.Width(60f));
                        GUILayout.BeginVertical();
                        Vector4 oldVal = p.Value, newVal;
                        newVal.x = GUILayout.HorizontalSlider(oldVal.x, 0f, 1f);
                        newVal.y = GUILayout.HorizontalSlider(oldVal.y, 0f, 1f);
                        newVal.z = GUILayout.HorizontalSlider(oldVal.z, 0f, 1f);
                        newVal.w = GUILayout.HorizontalSlider(oldVal.w, 0f, 1f);
                        if (newVal != oldVal) p.Value = newVal;
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical();
                        GUILayout.Label(oldVal.x.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.y.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.z.ToString("F"), GUILayout.Width(40f));
                        GUILayout.Label(oldVal.w.ToString("F"), GUILayout.Width(40f));
                        GUILayout.EndVertical();
                        if (GUILayout.Button("RESET", guiStyleButtonAlert)) p.Value = p.DefaultValue;
                        GUILayout.EndHorizontal();
                        GUILayout.Space(2f);
                    });
            }
        }

        private void GUITabView()
        {
            GUILayout.BeginVertical();
            control.UiBoolVals["canPreview"] = GUILayout.Toggle(control.UiBoolVals["canPreview"], "Auto-Preview",
                GUILayout.ExpandWidth(true));
            GUILayout.EndHorizontal();
            var r = GUILayoutUtility.GetRect(0, windowSize.width, 0, windowSize.height);
            if (Event.current.type == EventType.Repaint)
                orthoViewRect = r;
            var texture = control.Texture;
            if (texture)
                GUI.DrawTexture(orthoViewRect, texture, ScaleMode.ScaleToFit, false); // ALPHA BLENDING?! HEY HEY
        }

        private void GUITabConfig()
        {
            windowScrollPos = GUILayout.BeginScrollView(windowScrollPos, false, true);
            foreach (var ol in control.Config.Config)
            {
                GUILayout.BeginVertical("box");
                GUILayout.Label("<b>" + ol.Name + "</b>", "box");
                foreach (var o in ol.Options)
                {
                    GUILayout.BeginHorizontal();
                    if (o.IsToggle)
                        o.valueActive = GUILayout.Toggle(o.valueActive, o.Name);
                    else
                        GUILayout.Label(o.Name);
                    if (o.HasParam)
                    {
                        var displayText = o.valueParam.ToString(o.valueFormat);
                        displayText = GUILayout.TextField(displayText);
                        float value;
                        if (float.TryParse(displayText, out value))
                            o.valueParam = value;
                    }
                    GUILayout.EndHorizontal();
                }
                GUILayout.EndVertical();
                GUILayout.Space(5);
            }
            GUILayout.EndScrollView();
        }

        private void OnGUIAppLauncherReady()
        {
#if DEBUG
            Debug.Log(string.Format("KVV: OnGUIAppLauncherReady {0}", ApplicationLauncher.Ready));
#endif
            if (ApplicationLauncher.Ready)
                KVVButton = ApplicationLauncher.Instance.AddModApplication(
                    onAppLaunchToggleOn,
                    onAppLaunchToggleOff,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    DummyVoid,
                    ApplicationLauncher.AppScenes.SPH | ApplicationLauncher.AppScenes.VAB,
                    GameDatabase.Instance.GetTexture("VesselViewer/Textures/icon_button", false));
        }

        private void onAppLaunchToggleOn()
        {
            axis = EditorLogic.fetch.editorCamera.gameObject.AddComponent<EditorAxis>();
            control.UpdateShipBounds();
            visible = true;
        }

        private void onAppLaunchToggleOff()
        {
            DestroyObject(axis);
            visible = false;
        }

        private void DummyVoid()
        {
        }

        private void OnDestroy()
        {
#if DEBUG
            Debug.Log("KVV: OnDestroy");
#endif
            GameEvents.onGUIApplicationLauncherReady.Remove(OnGUIAppLauncherReady);

            if (KVVButton != null)
                ApplicationLauncher.Instance.RemoveModApplication(KVVButton);

            if (axis != null)
                DestroyObject(axis);

            if (control != null)
                DestroyObject(control);

            Resources.UnloadUnusedAssets();
        }
    }
}