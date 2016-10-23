using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer.Services
{
    public interface IScreenshotService
    {
        void Save(RenderTexture texture, float scalar);
    }
}
