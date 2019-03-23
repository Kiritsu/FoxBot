using LiteDB;

namespace Fox.Databases.Entities
{
    public class Entity
    {
        [BsonId(false)]
        public ulong Id { get; set; }
    }
}
