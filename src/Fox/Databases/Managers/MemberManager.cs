using System;
using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Databases.Entities;
using Fox.Databases.Interfaces;

namespace Fox.Databases.Managers
{
    public sealed class MemberManager : Manager<MemberEntity>, IGuildEntityManager<MemberEntity>
    {
        private readonly FoxDb _db;

        public MemberManager(FoxDb db) : base(db)
        {
            _db = db;
        }

        public MemberEntity GetOrCreate(SnowflakeObject sf, ulong guildId)
        {
            if (!(sf is DiscordMember mbr))
            {
                throw new InvalidOperationException("Given entity was not a DiscordMember.");
            }

            if (CachedEntities.TryGetValue(mbr.Id, out var entity))
            {
                _db.Logger.Print(LogLevel.Debug, "Database", $"GET_FROM_CACHE://member/{mbr.Id}");
                return entity;
            }

            entity = Get(mbr.Id);

            if (!(entity is null))
            {
                if (!CachedEntities.TryAdd(mbr.Id, entity))
                {
                    throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
                }

                return entity;
            }

            Create(new MemberEntity
            {
                Id = mbr.Id,
                RewardCooldown = DateTime.Now,
                Experience = 0,
                GuildId = guildId
            });

            entity = Get(mbr.Id);

            if (!CachedEntities.TryAdd(mbr.Id, entity))
            {
                throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
            }

            return entity;
        }

        public MemberEntity GetOrCreate(SnowflakeObject sf, DiscordGuild guild)
        {
            return GetOrCreate(sf, guild.Id);
        }
    }
}
