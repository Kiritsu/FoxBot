using System;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Databases.Entities;
using Fox.Databases.Entities.Models;
using Fox.Databases.Interfaces;

namespace Fox.Databases.Managers
{
    public sealed class GuildManager : Manager<GuildEntity>, IEntityManager<GuildEntity>
    {
        private readonly FoxDb _db;

        public GuildManager(FoxDb db) : base(db)
        {
            _db = db;
        }

        public GuildEntity GetOrCreate(SnowflakeObject sf)
        {
            if (!(sf is DiscordGuild gld))
            {
                throw new InvalidOperationException("Given entity was not a DiscordGuild.");
            }

            if (CachedEntities.TryGetValue(gld.Id, out var entity))
            {
                _db.Logger.Print(LogLevel.Debug, "Database", $"GET_FROM_CACHE://guild/{gld.Id}");
                return entity;
            }

            entity = Get(gld.Id);

            if (!(entity is null))
            {
                if (!CachedEntities.TryAdd(gld.Id, entity))
                {
                    throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
                }

                return entity;
            }

            Create(new GuildEntity
            {
                Id = gld.Id,
                Prefixes = new List<string>
                {
                    _db.Configuration.Prefix
                },
                Levenshtein = true,
                Music = new MusicConfiguration
                {
                    Volume = 50,
                    DjOnly = false,
                    Loop = false
                },
                CustomCommands = new List<CustomCommand>()
            });

            entity = Get(gld.Id);

            if (!CachedEntities.TryAdd(gld.Id, entity))
            {
                throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
            }

            return entity;
        }
    }
}
