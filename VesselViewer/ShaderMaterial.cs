using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VesselViewer
{
    internal class ShaderMaterial
    {
        private readonly List<ShaderMaterialProperty> properties;
        private readonly Dictionary<string, ShaderMaterialProperty> propertiesByName;

        private ShaderMaterial()
        {
            properties = new List<ShaderMaterialProperty>();
            propertiesByName = new Dictionary<string, ShaderMaterialProperty>();
            Enabled = true;
        }


        public ShaderMaterial(string shaderName)
            : this()
        {
            Material = new Material(Shader.Find(shaderName));
            var p = @"Properties\s*\{[^\{\}]*(((?<Open>\{)[^\{\}]*)+((?<Close-Open>\})[^\{\}]*)+)*(?(Open)(?!))\}";
            var m = Regex.Match(shaderName, p, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                throw new Exception("Error parsing shader properties: " + Material.shader.name);
            }
            p =
                @"(?<name>\w*)\s*\(\s*""(?<displayname>[^""]*)""\s*,\s*(?<type>Float|Vector|Color|2D|Rect|Cube|Range\s*\(\s*(?<rangemin>[\d.]*)\s*,\s*(?<rangemax>[\d.]*)\s*\))\s*\)";
            MonoBehaviour.print("1 " + m.Value);
            foreach (Match match in Regex.Matches(m.Value, p))
            {
                ShaderMaterialProperty prop;
                var name = match.Groups["name"].Value;
                var displayname = match.Groups["displayname"].Value;
                var typestr = match.Groups["type"].Value;
                switch (typestr.ToUpperInvariant())
                {
                    case "VECTOR":
                        prop = new ShaderMaterialProperty.VectorProperty(Material, name, displayname);
                        break;
                    case "COLOR":
                        prop = new ShaderMaterialProperty.ColorProperty(Material, name, displayname);
                        break;
                    case "2D":
                    case "RECT":
                    case "CUBE":
                        prop = new ShaderMaterialProperty.TextureProperty(Material, name, displayname);
                        break;
                    default: /// Defaults to Range(*,*)
                        prop = new ShaderMaterialProperty.FloatProperty(Material, name, displayname,
                            float.Parse(match.Groups["rangemin"].Value), float.Parse(match.Groups["rangemax"].Value));
                        break;
                }
                properties.Add(prop);
                propertiesByName[prop.Name] = prop;
            }
        }

        public Material Material { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public bool Enabled { get; set; }

        public ShaderMaterialProperty this[int propertyIndex]
        {
            get { return properties[propertyIndex]; }
            set { properties[propertyIndex] = value; }
        }

        public ShaderMaterialProperty this[string propertyName]
        {
            get { return propertiesByName[propertyName]; }
            set { propertiesByName[propertyName] = value; }
        }

        public int PropertyCount
        {
            get { return properties.Count; }
        }

        public ShaderMaterial Clone()
        {
            var result = new ShaderMaterial();
            result.Material = new Material(Material);
            foreach (var p in properties)
            {
                result.properties.Add(p.Clone());
            }
            return result;
        }
    }
}