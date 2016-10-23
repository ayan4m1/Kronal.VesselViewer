using System;

namespace VesselViewer.Config
{
    internal class VesselElementViewOption
    {
        public bool valueActive;
        public string valueFormat;
        public float valueParam;

        //constructor
        public VesselElementViewOption(string name, bool isToggle, bool hasParam,
            Action<VesselElementViewOptions, VesselElementViewOption, Part> apply,
            bool defaultValueActive = false, float defaultValueParam = 0f,
            string valueFormat = "F2")
        {
            Name = name;
            IsToggle = isToggle;
            HasParam = hasParam;
            Apply = apply;
            valueActive = defaultValueActive;
            valueParam = defaultValueParam;
            this.valueFormat = valueFormat;
        }

        public string Name { get; private set; }
        public bool IsToggle { get; private set; }
        public bool HasParam { get; private set; }
        public Action<VesselElementViewOptions, VesselElementViewOption, Part> Apply { get; private set; }
    }
}