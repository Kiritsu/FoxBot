using Fox.Databases.Entities;
using Fox.Databases.Interfaces;
using LiteDB;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using DSharpPlus;

namespace Fox.Databases.Managers
{
    public class Manager<T> : IManager<T> where T : Entity
    {
        public ConcurrentDictionary<ulong, T> CachedEntities { get; set; }

        private FoxDb Db { get; }

        private LiteCollection<T> Collection { get; }

        public Manager(FoxDb db)
        {
            Db = db;
            Collection = Db.GetLiteCollection<T>();
            CachedEntities = new ConcurrentDictionary<ulong, T>();
        }

        public T Get(ulong id)
        {
            Db.Logger.Print(LogLevel.Debug, "Database", $"GET://{Collection.Name}/{id}");
            return Collection.FindOne(x => x.Id == id);
        }

        public IEnumerable<T> Get(Expression<Func<T, bool>> predicate)
        {
            Db.Logger.Print(LogLevel.Debug, "Database", $"GET_BY_PREDICATE://{Collection.Name}");
            return Collection.Find(predicate);
        }

        public IEnumerable<T> GetAll()
        {
            Db.Logger.Print(LogLevel.Debug, "Database", $"GET_ALL://{Collection.Name}");
            return Collection.FindAll();
        }

        public BsonValue Create(T entity)
        {
            Db.Logger.Print(LogLevel.Debug, "Database", $"CREATE://{Collection.Name}/{entity.Id}");
            return Collection.Insert(entity);
        }

        public bool Remove(T entity)
        {
            Db.Logger.Print(LogLevel.Debug, "Database", $"DELETE://{Collection.Name}/{entity.Id}");
            return Collection.Delete(entity.Id);
        }

        public int Remove(Expression<Func<T, bool>> predicate)
        {
            Db.Logger.Print(LogLevel.Debug, "Database", $"DELETE_BY_PREDICATE://{Collection.Name}");
            return Collection.Delete(predicate);
        }

        public bool Update(T entity)
        {
            Db.Logger.Print(LogLevel.Debug, "Database", $"UPDATE://{Collection.Name}/{entity.Id}");
            return Collection.Update(entity);
        }
    }
}
