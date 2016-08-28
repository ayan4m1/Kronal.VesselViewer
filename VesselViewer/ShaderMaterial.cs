using System.Collections.Generic;
using UnityEngine;

namespace VesselViewer
{
    class ShaderMaterial
    {
        public Material Material { get; private set; }
        public string Name { get; private set; }
        public string FullName { get; private set; }
        public bool Enabled { get; set; }
        private List<ShaderMaterialProperty> properties;
        private Dictionary<string, ShaderMaterialProperty> propertiesByName;
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
        public int PropertyCount { get { return properties.Count; } }

        private ShaderMaterial()
        {
            properties = new List<ShaderMaterialProperty>();
            propertiesByName = new Dictionary<string, ShaderMaterialProperty>();
            Enabled = true;
        }


        public ShaderMaterial(string contents)
            : this()
        {
            /*
            this.Material = new Material(contents);
            var p = @"Properties\s*\{[^\{\}]*(((?<Open>\{)[^\{\}]*)+((?<Close-Open>\})[^\{\}]*)+)*(?(Open)(?!))\}";
            var m = Regex.Match(contents, p, RegexOptions.Multiline | RegexOptions.IgnoreCase);
            if (!m.Success)
            {
                throw new Exception("Error parsing shader properties: " + this.Material.shader.name);
            }
            p = @"(?<name>\w*)\s*\(\s*""(?<displayname>[^""]*)""\s*,\s*(?<type>Float|Vector|Color|2D|Rect|Cube|Range\s*\(\s*(?<rangemin>[\d.]*)\s*,\s*(?<rangemax>[\d.]*)\s*\))\s*\)";
            
    */
#if DEBUG
            //Debug.Log(string.Format("KVV: ShaderMaterial1 " + m.Value));
#endif
            /*
            foreach(Match match in Regex.Matches(m.Value, p))
            {
                ShaderMaterialProperty prop;
                var name = match.Groups["name"].Value;
                var displayname = match.Groups["displayname"].Value;
                var typestr = match.Groups["type"].Value;
                switch (typestr.ToUpperInvariant())
                {
                    case "VECTOR":
                        prop = new ShaderMaterialProperty.VectorProperty(this.Material, name, displayname);
                        break;
                    case "COLOR":
                        prop = new ShaderMaterialProperty.ColorProperty(this.Material, name, displayname);
                        break;
                    case "2D":
                    case "RECT":
                    case "CUBE":
                        prop = new ShaderMaterialProperty.TextureProperty(this.Material, name, displayname);
                        break;
                    default: /// Defaults to Range(*,*)
                        prop = new ShaderMaterialProperty.FloatProperty(this.Material, name, displayname, float.Parse(match.Groups["rangemin"].Value), float.Parse(match.Groups["rangemax"].Value));
                        break;
                }
                this.properties.Add(prop);
                this.propertiesByName[prop.Name] = prop;
            }
            */
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