using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace VesselViewer.Services
{
    public class PartReplaceService : IPartReplaceService
    {
        private readonly Dictionary<Part, Dictionary<MeshRenderer, Shader>> _cache =
            new Dictionary<Part, Dictionary<MeshRenderer, Shader>>();

        public void Replace(Part part)
        {
            var materials = KrsUtils.MaterialCache;
            var model = part.transform.Find("model");
            if (!model) return;

            var meshLibrary = new Dictionary<MeshRenderer, Shader>();
            foreach (var mesh in model.GetComponentsInChildren<MeshRenderer>())
            {
                var material = mesh.material;
                var shader = material.shader;

                Material replacement;
                if (materials.TryGetValue(shader.name, out replacement))
                {
                    //Debug.Log("KVV: Looking for replacement for " + shader.name);
                    mesh.material.shader = replacement.shader;

                    // store current for later restoration
                    if (!meshLibrary.ContainsKey(mesh))
                    {
                        meshLibrary.Add(mesh, shader);
                    }
                }

#if DEBUG
                if (replacement == null)
                {
                    Debug.LogWarning("KVV: No replacement for " + shader.name);
                }
#endif
            }

            if (!_cache.ContainsKey(part))
            {
                _cache.Add(part, meshLibrary);
            }
        }

        public void Restore(Part part)
        {
            var model = part.transform.Find("model");
            if (!model) return;

            Dictionary<MeshRenderer, Shader> savedMats;
            if (!_cache.TryGetValue(part, out savedMats)) return;

            foreach (var mesh in model.GetComponentsInChildren<MeshRenderer>())
            {
                Shader oldShader;
                if (savedMats.TryGetValue(mesh, out oldShader))
                    mesh.material.shader = oldShader;
            }
        }
    }
}
