using System;

namespace VesselViewer
{
    internal class VesselElementViewOption
    {
        public string Name { get; private set; }
        public bool IsToggle { get; private set; }
        public bool HasParam { get; private set; }
        public Action<VesselElementViewOptions, VesselElementViewOption, Part> Apply { get; private set; }
        public bool valueActive;
        public float valueParam;
        public string valueFormat;

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
    }
}