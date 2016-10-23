using System;
using System.Collections.Generic;

namespace VesselViewer.Config
{
    internal class VesselElementViewOptions
    {
        //constructor
        public VesselElementViewOptions(string name, Func<Part, bool> canApply)
        {
            Name = name;
            CanApply = canApply;
            Options = new List<VesselElementViewOption>();
        }

        public string Name { get; private set; }
        public Func<Part, bool> CanApply { get; }
        public List<VesselElementViewOption> Options { get; }


        internal void Apply(Part part)
        {
            if (!CanApply(part)) return;

            foreach (var option in Options)
                if (option.valueActive)
                    option.Apply(this, option, part);
        }
    }
}