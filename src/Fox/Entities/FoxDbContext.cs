using Fox.Databases.Entities;

namespace Fox.Entities
{
    public sealed class FoxDbContext
    {
        public FoxContext Context { get; }

        public GuildEntity Guild { get; }

        public MemberEntity Member { get; }

        public UserEntity User { get; }

        public ChannelEntity Channel { get; }

        public FoxDbContext(FoxContext context)
        {
            Context = context;

            var db = context.Database;

            Guild = db.GuildManager.GetOrCreate(context.Guild);
            Member = db.MemberManager.GetOrCreate(context.Member, context.Guild.Id);
            User = db.UserManager.GetOrCreate(context.User);
            Channel = db.ChannelManager.GetOrCreate(context.Channel);
        }

        public void UpdateGuild()
        {
            Context.Database.GuildManager.Update(Guild);
        }

        public void UpdateChannel()
        {
            Context.Database.ChannelManager.Update(Channel);
        }

        public void UpdateUser()
        {
            Context.Database.UserManager.Update(User);
        }

        public void UpdateMember()
        {
            Context.Database.MemberManager.Update(Member);
        }
    }
}
