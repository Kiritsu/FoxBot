namespace Fox.Databases.Entities.Models
{
    public class CustomCommand
    {
        public string Name { get; set; }

        public string Response { get; set; }

        public ulong AuthorId { get; set; }

        public ulong GuildId { get; set; }

        public int TimeUsed { get; set; }

        public bool Self { get; set; }
    }
}
