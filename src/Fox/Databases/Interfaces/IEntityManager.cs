using DSharpPlus.Entities;
using Fox.Databases.Entities;

namespace Fox.Databases.Interfaces
{
    public interface IEntityManager<T> : IManager<T> where T : Entity
    {
        T GetOrCreate(SnowflakeObject sf);
    }
}
