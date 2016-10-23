using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer.Services
{
    public interface ICameraService
    {
        void Initialize();
        void Update(Vector3 direction, Bounds? imageSize);
    }
}
