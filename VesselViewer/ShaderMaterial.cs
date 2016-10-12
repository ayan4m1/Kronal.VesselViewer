using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace VesselViewer
{
    class ShaderMaterial
    {
        public Material Material { get; private set; }
        public string Name { get; }
        public string FullName { get; }
        public bool Enabled { get; set; }

        private readonly List<ShaderMaterialProperty> _properties;
        private readonly Dictionary<string, ShaderMaterialProperty> _propertiesByName;

        public ShaderMaterialProperty this[int propertyIndex]
        {
            get { return _properties[propertyIndex]; }
            set { _properties[propertyIndex] = value; }
        }

        public ShaderMaterialProperty this[string propertyName]
        {
            get { return _propertiesByName[propertyName]; }
            set { _propertiesByName[propertyName] = value; }
        }

        public int PropertyCount => _properties.Count;

        public ShaderMaterial(string name, string fullName)
        {
            Name = name;
            FullName = fullName;
            Enabled = true;
            _properties = new List<ShaderMaterialProperty>();
            _propertiesByName = new Dictionary<string, ShaderMaterialProperty>();
        }

        public ShaderMaterial Clone()
        {
            var result = new ShaderMaterial(Name, FullName)
            {
                Material = new Material(Material)
            };

            foreach (var p in _properties)
            {
                result._properties.Add(p.Clone());
            }

            return result;
        }
    }
}