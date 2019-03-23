using DSharpPlus.Entities;
using Fox.Databases.Entities;

namespace Fox.Databases.Interfaces
{
    public interface IGuildEntityManager<T> : IManager<T> where T : Entity
    {
        T GetOrCreate(SnowflakeObject sf, ulong guildId);
        T GetOrCreate(SnowflakeObject sf, DiscordGuild guild);
    }
}
