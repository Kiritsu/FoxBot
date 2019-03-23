using System;
using System.Collections.Generic;
using DSharpPlus;
using DSharpPlus.Entities;
using Fox.Databases.Entities;
using Fox.Databases.Entities.Models;
using Fox.Databases.Interfaces;

namespace Fox.Databases.Managers
{
    public sealed class UserManager : Manager<UserEntity>, IEntityManager<UserEntity>
    {
        private readonly FoxDb _db;

        public UserManager(FoxDb db) : base(db)
        {
            _db = db;
        }

        public UserEntity GetOrCreate(SnowflakeObject sf)
        {
            if (!(sf is DiscordUser usr))
            {
                throw new InvalidOperationException("Given entity was not a DiscordUser.");
            }

            if (CachedEntities.TryGetValue(usr.Id, out var entity))
            {
                _db.Logger.Print(LogLevel.Debug, "Database", $"GET_FROM_CACHE://user/{usr.Id}");
                return entity;
            }

            entity = Get(usr.Id);

            if (!(entity is null))
            {
                if (!CachedEntities.TryAdd(usr.Id, entity))
                {
                    throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
                }

                return entity;
            }

            Create(new UserEntity
            {
                Id = usr.Id,
                RewardCooldown = DateTime.Now,
                Gold = 0,
                IsBlacklisted = false,
                Experience = 0,
                Playlists = new List<UserPlaylist>()
            });

            entity = Get(usr.Id);

            if (!CachedEntities.TryAdd(usr.Id, entity))
            {
                throw new InvalidOperationException("The object was already present in cache but was not pulled earlier. This should not happen.");
            }

            return entity;
        }
    }
}
