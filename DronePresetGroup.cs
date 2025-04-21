using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PhotomodeMultiview
{
    public class DronePresetGroup
    {
        public string Name { get; private set; }
        public List<DronePreset> Presets { get; private set; }

        public DronePresetGroup(string name)
        {
            Name = name;
            Presets = new List<DronePreset>();
        }
    }

}
