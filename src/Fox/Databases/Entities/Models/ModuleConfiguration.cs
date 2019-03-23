using DSharpPlus;
using Fox.Databases.Enums;

namespace Fox.Databases.Entities.Models
{
    public class ModuleConfiguration
    {
        public string Name { get; set; }

        public ModuleState State { get; set; }

        public Permissions Permissions { get; set; }
    }
}
