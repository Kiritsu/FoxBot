using System.Collections.Concurrent;
using Fox.Databases.Entities;

namespace Fox.Databases.Interfaces
{
    public interface ICachedEntity<T> where T : Entity
    {
        ConcurrentDictionary<ulong, T> CachedEntities { get; set; }
    }
}
