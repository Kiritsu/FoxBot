using System.Collections.Concurrent;
using Fox.Databases.Entities.Models;
using MingweiSamuel.Camille.Enums;

namespace Fox.Databases.Entities
{
    public sealed class ChannelEntity : Entity
    {
        public ConcurrentDictionary<string, ModuleConfiguration> Modules { get; set; }

        public string RiotRegion { get; set; }

        public Region Region => Region.Get(RiotRegion);
    }
}
