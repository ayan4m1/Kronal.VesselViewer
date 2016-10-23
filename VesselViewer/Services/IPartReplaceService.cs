using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer.Services
{
    public interface IPartReplaceService
    {
        void Replace(Part part);
        void Restore(Part part);
    }
}
