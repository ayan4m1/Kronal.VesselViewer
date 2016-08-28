using System;
using UnityEngine;

namespace VesselViewer
{
    internal abstract class ShaderMaterialProperty
    {
        private ShaderMaterialProperty(Material material, string name, string displayName)
        {
            Material = material;
            Name = name;
            DisplayName = displayName;
        }

        public readonly Material Material;
        public string Name { get; private set; }
        public string DisplayName { get; private set; }
        public abstract void Match(
            Action<FloatProperty> IfFloat = null,
            Action<VectorProperty> IfVector = null,
            Action<ColorProperty> IfColor = null,
            Action<TextureProperty> IfTexture = null);

        public abstract ShaderMaterialProperty Clone();

        public sealed class FloatProperty : ShaderMaterialProperty
        {
            public float Value
            {
                get { return Material.GetFloat(Name); }
                set { Material.SetFloat(Name, value); }
            }
            public readonly float RangeMin;
            public readonly float RangeMax;
            public readonly float DefaultValue;

            internal FloatProperty(Material material, string name, string displayName, float min, float max)
                : base(material, name, displayName)
            {
                RangeMin = min;
                RangeMax = max;
                DefaultValue = Value;
            }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfFloat != null) IfFloat(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new FloatProperty(Material, Name, DisplayName, RangeMin, RangeMax);
            }
        }

        public sealed class VectorProperty : ShaderMaterialProperty
        {
            public Vector4 Value
            {
                get { return Material.GetVector(Name); }
                set { Material.SetVector(Name, value); }
            }
            public readonly Vector4 DefaultValue;

            internal VectorProperty(Material material, string name, string displayName)
                : base(material, name, displayName)
            {
                DefaultValue = Value;
            }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfVector != null) IfVector(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new VectorProperty(Material, Name, DisplayName);
            }
        }

        public sealed class ColorProperty : ShaderMaterialProperty
        {
            public Color Value
            {
                get { return Material.GetColor(Name); }
                set { Material.SetColor(Name, value); }
            }
            public readonly Color DefaultValue;

            internal ColorProperty(Material material, string name, string displayName)
                : base(material, name, displayName)
            {
                DefaultValue = Value;
            }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfColor != null) IfColor(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new VectorProperty(Material, Name, DisplayName);
            }
        }

        public sealed class TextureProperty : ShaderMaterialProperty
        {
            public Texture Value
            {
                get { return Material.GetTexture(Name); }
                set { Material.SetTexture(Name, value); }
            }

            internal TextureProperty(Material material, string name, string displayName)
                : base(material, name, displayName) { }

            public override void Match(
                Action<FloatProperty> IfFloat = null,
                Action<VectorProperty> IfVector = null,
                Action<ColorProperty> IfColor = null,
                Action<TextureProperty> IfTexture = null)
            {
                if (IfTexture != null) IfTexture(this);
            }

            public override ShaderMaterialProperty Clone()
            {
                return new TextureProperty(Material, Name, DisplayName);
            }
        }
    }
}