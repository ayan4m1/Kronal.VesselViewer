using System;
using System.Collections.Generic;
using KAS;
using Keramzit;
using UnityEngine;

namespace VesselViewer
{
    internal class VesselViewConfig
    {
        private readonly Dictionary<Part, bool> freezed;
        public List<string> installedMods = new List<string>();
        public Action onApply;
        public Action onRevert;
        private readonly Dictionary<Transform, Vector3> positions;
        public float procFairingOffset;
        public Dictionary<Part, bool> procFairings;
        private IShipconstruct ship;
        private readonly Dictionary<Renderer, bool> visibility;

        //constructor
        public VesselViewConfig()
        {
            buildModList();
            positions = new Dictionary<Transform, Vector3>();
            visibility = new Dictionary<Renderer, bool>();
            freezed = new Dictionary<Part, bool>();
            procFairings = new Dictionary<Part, bool>();
            onApply = () => { };
            onRevert = () => { };
            Config = new List<VesselElementViewOptions>
            {
                new VesselElementViewOptions("Stack Decouplers/Separators", CanApplyIfModule("ModuleDecouple"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, StackDecouplerExplode, true, 1f)
                    }
                },
                new VesselElementViewOptions("Radial Decouplers/Separators", CanApplyIfModule("ModuleAnchoredDecoupler"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, RadialDecouplerExplode, true, 1f)
                    }
                },
                new VesselElementViewOptions("Docking Ports", CanApplyIfModule("ModuleDockingNode"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, DockingPortExplode, true, 1f)
                    }
                },
                new VesselElementViewOptions("Engine Fairings", CanApplyIfModule("ModuleJettison"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, EngineFairingExplode, true, 1f),
                        new VesselElementViewOption("Hide", true, false, EngineFairingHide, true)
                    }
                }
            };
            if (hasMod("KAS"))
                Config.Add(new VesselElementViewOptions("KAS Connector Ports", CanApplyIfModule("KASModulePort"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, KASConnectorPortExplode, true, 1f)
                    }
                });
            if (hasMod("ProceduralFairings"))
                Config.Add(new VesselElementViewOptions("Procedural Fairings", CanApplyIfModule("ProceduralFairingSide"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, ProcFairingExplode, false, 3f),
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive, false),
                        new VesselElementViewOption("Hide front half", true, false, ProcFairingHide, false)
                    }
                });

            Config.Add(new VesselElementViewOptions("Struts", CanApplyIfType("StrutConnector"))
            {
                Options =
                {
                    new VesselElementViewOption("Hide", true, false, PartHideRecursive, true)
                }
            });
            Config.Add(new VesselElementViewOptions("Launch Clamps", CanApplyIfModule("LaunchClamp"))
            {
                Options =
                {
                    new VesselElementViewOption("Hide", true, false, PartHideRecursive, true)
                }
            });
        }

        public List<VesselElementViewOptions> Config { get; }

        public void buildModList()
        {
            //https://github.com/Xaiier/Kreeper/blob/master/Kreeper/Kreeper.cs#L92-L94 <- Thanks Xaiier!
            foreach (var a in AssemblyLoader.loadedAssemblies)
            {
                var name = a.name;
//UnityEngine.Debug.Log(string.Format("KVV: name: {0}", name));//future debubging
                installedMods.Add(name);
            }
        }

        public bool hasMod(string modIdent)
        {
            return installedMods.Contains(modIdent);
        }

        private void StateToggle(bool toggleOn)
        {
            var p = EditorLogic.RootPart;
            if (toggleOn)
            {
                positions.Clear();
                visibility.Clear();
                freezed.Clear();
                procFairings.Clear();
            }
            foreach (var t in p.GetComponentsInChildren<Transform>())
                if (toggleOn)
                    positions[t] = t.localPosition;
                else if (!toggleOn && positions.ContainsKey(t))
                    t.localPosition = positions[t];
            foreach (var r in p.GetComponentsInChildren<Renderer>())
                if (toggleOn)
                    visibility[r] = r.enabled;
                else if (!toggleOn && visibility.ContainsKey(r))
                    r.enabled = visibility[r];
            foreach (var part in ship.Parts)
            {
                if (toggleOn)
                    freezed[part] = part.frozen;
                else if (!toggleOn && freezed.ContainsKey(part))
                    part.frozen = freezed[part];

                if (hasMod("ProceduralFairings"))
                    proceduralFairingToggleState(toggleOn, part);
            }
            if (!toggleOn)
                onRevert();
            //else { this.onSaveState(); }
        }

        private void SaveState()
        {
            StateToggle(true);
        }

        public void Revert()
        {
            StateToggle(false);
        }

        public void Execute(IShipconstruct ship)
        {
            this.ship = ship;
            StateToggle(false);
            StateToggle(true);
            foreach (var part in ship.Parts)
            {
                foreach (var c in Config)
                    c.Apply(part);
                part.frozen = true;
            }

            onApply();
        }


        private void proceduralFairingToggleState(bool toggleOn, Part part)
        {
            if (part.Modules.Contains("ProceduralFairingSide") && hasMod("ProceduralFairings"))
            {
                var module = part.Module<ProceduralFairingSide>();

                // Preserve ship's original fairing lock state.
                if (toggleOn && !module.shapeLock)
                {
                    module.shapeLock = true;
                    procFairings[part] = true;
                }
                else if (!toggleOn && procFairings.ContainsKey(part))
                {
                    module.shapeLock = false;
                }
            }
        }

        private Func<Part, bool> CanApplyIfType(string typeName)
        {
            var type = KrsUtils.FindType(typeName);
            return p => type.IsInstanceOfType(p);
        }

        private Func<Part, bool> CanApplyIfModule(string moduleName)
        {
            return p => p.Modules.Contains(moduleName);
        }

        private void PartHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            foreach (var r in part.GetComponents<Renderer>())
                r.enabled = false;
        }

        private void PartHideRecursive(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            foreach (var r in part.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        private void StackDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleDecouple>();
            if (module.isDecoupled) return;
            if (!module.staged) return;
            if (!part.parent) return;
            Vector3 dir;
            if (module.isOmniDecoupler)
                foreach (var c in part.children)
                {
                    dir = Vector3.Normalize(c.transform.position - part.transform.position);
                    c.transform.Translate(dir*o.valueParam, Space.World);
                }
            dir = Vector3.Normalize(part.transform.position - part.parent.transform.position);
            part.transform.Translate(dir*o.valueParam, Space.World);
        }


        private void RadialDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleAnchoredDecoupler>();
            if (module.isDecoupled) return;
            if (!module.staged) return;
            if (string.IsNullOrEmpty(module.explosiveNodeID)) return;
            var an = module.explosiveNodeID == "srf" ? part.srfAttachNode : part.FindAttachNode(module.explosiveNodeID);
            if ((an == null) || (an.attachedPart == null)) return;
            var distance = o.valueParam;
            if (part.name.Contains("FairingCone"))
                distance *= -1;
            Part partToBeMoved;
            if (an.attachedPart == part.parent)
            {
                distance *= -1;
                partToBeMoved = part;
            }
            else
            {
                partToBeMoved = an.attachedPart;
            }
            partToBeMoved.transform.Translate(part.transform.right*distance, Space.World);
        }

        private void DockingPortExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleDockingNode>();
            if (string.IsNullOrEmpty(module.referenceAttachNode)) return;
            var an = part.FindAttachNode(module.referenceAttachNode);
            if (!an.attachedPart) return;
            var distance = o.valueParam;
            Part partToBeMoved;
            if (an.attachedPart == part.parent)
            {
                distance *= -1;
                partToBeMoved = part;
            }
            else
            {
                partToBeMoved = an.attachedPart;
            }
            partToBeMoved.transform.Translate(module.nodeTransform.forward*distance, Space.World);
        }

        private void EngineFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleJettison>();
            if (!module.isJettisoned)
                if (!module.isFairing)
                    module.jettisonTransform.Translate(module.jettisonDirection*o.valueParam);
        }

        private void EngineFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleJettison>();
            if (module.jettisonTransform)
                foreach (var r in module.jettisonTransform.gameObject.GetComponentsInChildren<Renderer>())
                    r.enabled = false;
        }

        private void KASConnectorPortExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (hasMod("KAS"))
            {
                var module = part.Module<KASModulePort>(); //this creates KAS Dependancy.  
                if (string.IsNullOrEmpty(module.attachNode)) return;
                var an = part.FindAttachNode(module.attachNode);
                if (!an.attachedPart) return;
                var distance = o.valueParam;
                Part partToBeMoved;
                if (an.attachedPart == part.parent)
                {
                    distance *= -1;
                    partToBeMoved = part;
                }
                else
                {
                    partToBeMoved = an.attachedPart;
                }
                partToBeMoved.transform.Translate(module.portNode.forward*distance, Space.World);
            }
        }

        private void ProcFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (hasMod("ProceduralFairings"))
            {
                var nct = part.FindModelTransform("nose_collider");
                if (!nct) return;
                procFairingOffset = o.valueParam;
                var extents = new Vector3(o.valueParam, o.valueParam, o.valueParam);
                part.transform.Translate(Vector3.Scale(nct.right, extents), Space.World);
            }
        }

        private void ProcFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (hasMod("ProceduralFairings"))
            {
                var nct = part.FindModelTransform("nose_collider");
                if (!nct) return;
                var forward = EditorLogic.RootPart.transform.forward;
                var right = EditorLogic.RootPart.transform.right;

                if (Vector3.Dot(nct.right, -forward.normalized) > 0f)
                {
                    var renderer = part.GetComponentInChildren<Renderer>();
                    if (renderer) renderer.enabled = false;
                }
            }
        }
    }
}