using System.Threading.Tasks;
using DSharpPlus.Entities;
using Fox.Commands.Checks;
using Fox.Entities;
using Fox.Enums;
using Fox.Exceptions;
using Fox.Extensions;
using Qmmands;

namespace Fox.Commands.Modules
{
    [Group("Test"), Name("Test"), RequireOwner, CheckModuleState, Hidden]
    public sealed class TestModule : FoxModuleBase
    {
        [Command("Cooldown")]
        [Cooldown(1, 123456789, CooldownMeasure.Seconds, CooldownBucketType.Global)]
        public Task CooldownAsync()
        {
            return RespondAsync("cooooooooool...down.");
        }

        [Command("Cooldown")]
        [Cooldown(1, 123456789, CooldownMeasure.Seconds, CooldownBucketType.Global)]
        public Task CooldownAsync(int i)
        {
            return RespondAsync($"cooo[+{i}]l...down.");
        }

        [Command("BadResult")]
        public Task<FoxResult> BadResultAsync()
        {
            return Task.FromResult(new FoxResult(false, "Test of an unsuccessfull result."));
        }

        [Command("GoodResult")]
        public Task<FoxResult> GoodResultAsync()
        {
            return Task.FromResult(new FoxResult(true, "Test of a successfull result."));
        }

        [Command("Throw", "Throws")]
        public Task ThrowAsync()
        {
            throw new FoxException("Test of a throw during command runtime.");
        }

        [Command("SendMessage")]
        public Task SendMessageAsync(string message = null)
        {
            return RespondAsync(message ?? "Test message.");
        }

        [Group("Nested"), Name("Nested")]
        public sealed class NestedModule : FoxModuleBase
        {
            [Group("SubNested"), Name("SubNested")]
            public sealed class SubNestedModule : FoxModuleBase
            {
                [Command("owo")]
                public Task OwoAsync(DiscordMember mbr)
                {
                    return RespondAsync($"{mbr.FormatUser()}: what's this?");
                }
            }

            [Command("NestedCommand")]
            public Task NestedCommandAsync()
            {
                return RespondAsync("owo it's nested.");
            }
        }

        [Command("Lookup")]
        public Task LookupAsync(SkeletonUser skeleton)
        {
            return RespondAsync("Skeleton found: " + skeleton.FullName);
        }

        [Command("Lookup")]
        public Task LookupAsync(DiscordMember member)
        {
            return RespondAsync("DiscordMember found " + member);
        }

        [Command("Lookup")]
        public Task LookupAsync(DiscordUser user)
        {
            return RespondAsync("DiscordUser found " + user);
        }

        [Command("->")]
        public Task SeparatorTestAsync()
        {
            return RespondAsync("It worked.");
        }

        [Command("Gold")]
        public Task GoldAsync()
        {
            return RespondAsync($"You have {DbContext.User.Gold} gold.");
        }

        [Command("SetGold")]
        public Task SetGoldAsync(int amount)
        {
            DbContext.User.Gold = amount;
            Db.UserManager.Update(DbContext.User);

            return RespondAsync($"Amount of gold updated, you have {DbContext.User.Gold} gold.");
        }
    }
}
