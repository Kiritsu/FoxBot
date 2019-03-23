using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Fox.Databases.Entities;
using LiteDB;

namespace Fox.Databases.Interfaces
{
    public interface IManager<T> : ICachedEntity<T> where T : Entity
    {
        T Get(ulong id);

        IEnumerable<T> Get(Expression<Func<T, bool>> predicate);

        IEnumerable<T> GetAll();

        BsonValue Create(T entity);

        bool Remove(T entity);

        int Remove(Expression<Func<T, bool>> predicate);

        bool Update(T entity);
    }
}
