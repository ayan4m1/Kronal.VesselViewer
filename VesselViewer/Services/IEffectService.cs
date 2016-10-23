using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer.Services
{
    public interface IEffectService
    {
        string[] GetNames();
        void Enable(string name);
        void Disable(string name);
        void ApplyEnabled(RenderTexture texture);
    }
}
