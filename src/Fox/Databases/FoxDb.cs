using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DSharpPlus;
using Fox.Databases.Entities;
using Fox.Databases.Managers;
using Fox.Services;
using LiteDB;

namespace Fox.Databases
{
    public sealed class FoxDb
    {
        private readonly IReadOnlyList<object> _dbEntities;

        public LogService Logger { get; }

        public ConfigurationService Configuration { get; }

        public GuildManager GuildManager { get; }

        public MemberManager MemberManager { get; }

        public UserManager UserManager { get; }

        public ChannelManager ChannelManager { get; }

        public FoxDb(LogService lgr, ConfigurationService cfg)
        {
            Logger = lgr;
            Configuration = cfg;

            LiteCollection<GuildEntity> lcGuild;
            LiteCollection<UserEntity> lcUser;
            LiteCollection<MemberEntity> lcMember;
            LiteCollection<ChannelEntity> lcChannel;

            if (!Directory.Exists("databases"))
            {
                Directory.CreateDirectory("databases");
            }

            using (var guilds = new LiteDatabase($"Filename=databases/guilds.db; Password={cfg.DbPassword}"))
            {
                lcGuild = guilds.GetCollection<GuildEntity>("guilds");
            }

            using (var users = new LiteDatabase($"Filename=databases/users.db; Password={cfg.DbPassword}"))
            {
                lcUser = users.GetCollection<UserEntity>("users");
            }

            using (var members = new LiteDatabase($"Filename=databases/members.db; Password={cfg.DbPassword}"))
            {
                lcMember = members.GetCollection<MemberEntity>("members");
            }

            using (var channels = new LiteDatabase($"Filename=databases/channels.db; Password={cfg.DbPassword}"))
            {
                lcChannel = channels.GetCollection<ChannelEntity>("channels");
            }

            var mapper = BsonMapper.Global;
            mapper.Entity<ChannelEntity>()
                .Ignore(x => x.Region);

            lcGuild.EnsureIndex(x => x.Id);
            lcUser.EnsureIndex(x => x.Id);
            lcMember.EnsureIndex(x => x.Id);
            lcChannel.EnsureIndex(x => x.Id);

            _dbEntities = new List<object>
            {
                lcGuild,
                lcUser,
                lcMember,
                lcChannel
            };

            GuildManager = new GuildManager(this);
            MemberManager = new MemberManager(this);
            UserManager = new UserManager(this);
            ChannelManager = new ChannelManager(this);

            lgr.Print(LogLevel.Info, "Database", "Database loaded with success.");
        }

        internal LiteCollection<T> GetLiteCollection<T>()
        {
            var lc = _dbEntities.FirstOrDefault(x => x is LiteCollection<T>);

            if (lc is null)
            {
                throw new InvalidOperationException("The given T type was not found in the collection of LiteColletion.");
            }

            return lc as LiteCollection<T>;
        }
    }
}
