using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer.Services
{
    class EffectService : IEffectService
    {
        public readonly Dictionary<string, ShaderMaterial> Effects = new Dictionary<string, ShaderMaterial>
        {
            ["Edge Detect"] = new ShaderMaterial("MaterialEdgeDetect", "Hidden/EdgeDetect"),
            ["FXAA"] = new ShaderMaterial("MaterialFXAA", "Hidden/FXAA3")
        };

        public string[] GetNames()
        {
            return Effects.Keys.ToArray();
        }

        public void Enable(string name)
        {
            SetState(name, true);
        }

        public void Disable(string name)
        {
            SetState(name, false);
        }

        private void SetState(string name, bool state)
        {
            if (!Effects.ContainsKey(name))
            {
                return;
            }

            Effects[name].Enabled = state;
        }

        public void ApplyEnabled(RenderTexture texture)
        {
            foreach (var fx in Effects)
            {
                if (fx.Value.Enabled && fx.Value.TryInitialize())
                {
                    Graphics.Blit(texture, texture, fx.Value.Material);
                }
            }
        }
    }
}
