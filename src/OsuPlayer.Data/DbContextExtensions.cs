using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Milki.OsuPlayer.Data;

public static class DbContextExtensions
{
    public static EntityEntry<TEntity> Update<TEntity>(
        this DbContext dbContext,
        TEntity entity,
        params Expression<Func<TEntity, object?>>[] propertyExpressions
    ) where TEntity : class
    {
        if (dbContext.Entry(entity).State != EntityState.Detached)
            throw new Exception("Entity shouldn't be attached");
        dbContext.Attach(entity);
        foreach (var propertyExpression in propertyExpressions)
        {
            dbContext.Entry(entity).Property(propertyExpression).IsModified = true;
        }

        return dbContext.Entry(entity);
    }

    public static async ValueTask<EntityEntry<TEntity>> UpdateAndSaveChangesAsync<TEntity>(
        this DbContext dbContext,
        TEntity entity,
        params Expression<Func<TEntity, object?>>[] propertyExpressions
    ) where TEntity : class
    {
        if (dbContext.Entry(entity).State != EntityState.Detached)
            throw new Exception("Entity shouldn't be attached");
        dbContext.Attach(entity);
        foreach (var propertyExpression in propertyExpressions)
        {
            dbContext.Entry(entity).Property(propertyExpression).IsModified = true;
        }

        await dbContext.SaveChangesAsync();
        return dbContext.Entry(entity);
    }
}