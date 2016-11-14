using System;
using System.Collections.Generic;
using Keramzit;
using UniLinq;
using UnityEngine;

namespace VesselViewer.Config
{
    internal class VesselViewConfig
    {
        public Dictionary<Part, bool> ProcFairings;
        public float ProcFairingOffset;

        public List<string> InstalledMods = new List<string>();
        public Action OnApply;
        public Action OnRevert;

        private IShipconstruct _ship;
        private readonly Dictionary<Part, bool> _freezed = new Dictionary<Part, bool>();
        private readonly Dictionary<Renderer, bool> _visibility = new Dictionary<Renderer, bool>();
        private readonly Dictionary<Transform, Vector3> _positions = new Dictionary<Transform, Vector3>();

        public VesselViewConfig()
        {
            BuildModList();
            ProcFairings = new Dictionary<Part, bool>();
            OnApply = () => { };
            OnRevert = () => { };
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
            if (HasMod("KAS"))
                Config.Add(new VesselElementViewOptions("KAS Connector Ports", CanApplyIfModule("KASModulePort"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, KasConnectorPortExplode, true, 1f)
                    }
                });
            if (HasMod("ProceduralFairings"))
                Config.Add(new VesselElementViewOptions("Procedural Fairings", CanApplyIfModule("ProceduralFairingSide"))
                {
                    Options =
                    {
                        new VesselElementViewOption("Offset", true, true, ProcFairingExplode, false, 3f),
                        new VesselElementViewOption("Hide", true, false, PartHideRecursive),
                        new VesselElementViewOption("Hide front half", true, false, ProcFairingHide)
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

        public void BuildModList()
        {
            //https://github.com/Xaiier/Kreeper/blob/master/Kreeper/Kreeper.cs#L92-L94 <- Thanks Xaiier!
            InstalledMods.AddRange(AssemblyLoader.loadedAssemblies.Select(asm => asm.name));
        }

        public bool HasMod(string modIdent)
        {
            return InstalledMods.Contains(modIdent);
        }

        private void StateToggle(bool toggleOn)
        {
            var p = EditorLogic.RootPart;
            if (toggleOn)
            {
                _positions.Clear();
                _visibility.Clear();
                _freezed.Clear();
                ProcFairings.Clear();
            }
            foreach (var t in p.GetComponentsInChildren<Transform>())
                if (toggleOn)
                    _positions[t] = t.localPosition;
                else if (_positions.ContainsKey(t))
                    t.localPosition = _positions[t];
            foreach (var r in p.GetComponentsInChildren<Renderer>())
                if (toggleOn)
                    _visibility[r] = r.enabled;
                else if (_visibility.ContainsKey(r))
                    r.enabled = _visibility[r];
            foreach (var part in _ship.Parts)
            {
                if (toggleOn)
                    _freezed[part] = part.frozen;
                else if (_freezed.ContainsKey(part))
                    part.frozen = _freezed[part];

                if (HasMod("ProceduralFairings"))
                    ProceduralFairingToggleState(toggleOn, part);
            }

            if (!toggleOn)
                OnRevert();
        }

        public void Revert()
        {
            StateToggle(false);
        }

        public void Execute(IShipconstruct ship)
        {
            _ship = ship;
            StateToggle(false);
            StateToggle(true);

            foreach (var part in ship.Parts)
            {
                foreach (var c in Config)
                    c.Apply(part);

                part.frozen = true;
            }

            OnApply();
        }


        private void ProceduralFairingToggleState(bool toggleOn, Part part)
        {
            if (!part.Modules.Contains("ProceduralFairingSide") || !HasMod("ProceduralFairings")) return;
            var module = part.Module<ProceduralFairingSide>();

            // Preserve ship's original fairing lock state.
            if (toggleOn && !module.shapeLock)
            {
                module.shapeLock = true;
                ProcFairings[part] = true;
            }
            else if (!toggleOn && ProcFairings.ContainsKey(part))
            {
                module.shapeLock = false;
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

        private static void PartHideRecursive(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            foreach (var r in part.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        public void StackDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleDecouple>();
            if (module.isDecoupled) return;
            if (!module.staged) return;
            if (!part.parent) return;
            Vector3 dir;
            if (module.isOmniDecoupler)
            {
                foreach (var c in part.children)
                {
                    dir = Vector3.Normalize(c.transform.position - part.transform.position);
                    c.transform.Translate(dir * o.valueParam, Space.World);
                }
            }
            dir = Vector3.Normalize(part.transform.position - part.parent.transform.position);
            part.transform.Translate(dir * o.valueParam, Space.World);
        }

        public void RadialDecouplerExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleAnchoredDecoupler>();
            if (module.isDecoupled || !module.staged || string.IsNullOrEmpty(module.explosiveNodeID)) return;

            var an = module.explosiveNodeID == "srf" ? part.srfAttachNode : part.FindAttachNode(module.explosiveNodeID);
            if (an?.attachedPart == null) return;

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

            partToBeMoved.transform.Translate(part.transform.right * distance, Space.World);
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

            partToBeMoved.transform.Translate(module.nodeTransform.forward * distance, Space.World);
        }

        private static void EngineFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleJettison>();
            if (!module.isJettisoned && !module.isFairing)
                module.jettisonTransform.Translate(module.jettisonDirection * o.valueParam);
        }

        public void EngineFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            var module = part.Module<ModuleJettison>();
            if (!module.jettisonTransform) return;

            foreach (var r in module.jettisonTransform.gameObject.GetComponentsInChildren<Renderer>())
                r.enabled = false;
        }

        private void KasConnectorPortExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (!HasMod("KAS")) return;
            var module = part.Module<KASModulePort>();
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
            partToBeMoved.transform.Translate(module.portNode.forward * distance, Space.World);
        }

        private void ProcFairingExplode(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (!HasMod("ProceduralFairings")) return;

            var noseCone = part.FindModelTransform("nose_collider");
            if (!noseCone) return;

            ProcFairingOffset = o.valueParam;
            var extents = new Vector3(o.valueParam, o.valueParam, o.valueParam);
            part.transform.Translate(Vector3.Scale(noseCone.right, extents), Space.World);
        }

        private void ProcFairingHide(VesselElementViewOptions ol, VesselElementViewOption o, Part part)
        {
            if (!HasMod("ProceduralFairings")) return;

            var noseCone = part.FindModelTransform("nose_collider");
            if (!noseCone) return;

            var forward = EditorLogic.RootPart.transform.forward;
            if (!(Vector3.Dot(noseCone.right, -forward.normalized) > 0f)) return;

            var renderer = part.GetComponentInChildren<Renderer>();
            if (renderer) renderer.enabled = false;
        }
    }
}