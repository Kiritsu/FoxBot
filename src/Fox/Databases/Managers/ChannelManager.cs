using System;
using System.Collections.Concurrent;
using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Databases.Entities;
using Fox.Databases.Entities.Models;
using Fox.Databases.Interfaces;
using MingweiSamuel.Camille.Enums;

namespace Fox.Databases.Managers
{
    public sealed class ChannelManager : Manager<ChannelEntity>, IEntityManager<ChannelEntity>
    {
        private readonly FoxDb _db;

        public ChannelManager(FoxDb db) : base(db)
        {
            _db = db;
        }

        public ChannelEntity GetOrCreate(SnowflakeObject sf)
        {
            if (!(sf is DiscordChannel chn))
            {
                throw new InvalidOperationException("Given entity was not a DiscordChannel.");
            }

            if (CachedEntities.TryGetValue(chn.Id, out var entity))
            {
                _db.Logger.Print(LogLevel.Debug, "Database", $"GET_FROM_CACHE://channel/{chn.Id}");
                return entity;
            }

            entity = Get(chn.Id);

            if (!(entity is null))
            {
                if (!CachedEntities.TryAdd(chn.Id, entity))
                {
                    throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
                }

                return entity;
            }

            Create(new ChannelEntity
            {
                Id = chn.Id,
                Modules = new ConcurrentDictionary<string, ModuleConfiguration>(),
                RiotRegion = "EUW"
            });

            entity = Get(chn.Id);

            if (!CachedEntities.TryAdd(chn.Id, entity))
            {
                throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
            }

            return entity;
        }
    }
}
