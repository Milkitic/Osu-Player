using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Milki.OsuPlayer.Data.Models;

namespace Milki.OsuPlayer.Data;

public abstract class DbContextBase : DbContext
{
    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        ModifyModelTimestamp();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    public override int SaveChanges()
    {
        ModifyModelTimestamp();
        return base.SaveChanges();
    }

    private void ModifyModelTimestamp()
    {
        foreach (var e in ChangeTracker.Entries())
        {
            if (e.Entity is IAutoCreatable creatable && e.State == EntityState.Added)
            {
                creatable.CreateTime = DateTime.Now;
            }
            else if (e.Entity is IAutoUpdatable updatable && e.State == EntityState.Modified)
            {
                updatable.UpdatedTime = DateTime.Now;
            }
        }
    }
}