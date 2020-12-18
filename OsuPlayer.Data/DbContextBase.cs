using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Milky.OsuPlayer.Data.Models;

namespace Milky.OsuPlayer.Data
{
    public class DbContextBase : DbContext
    {
        public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,
            CancellationToken cancellationToken = default)
        {
            this.ModifyModelTimestamp();
            return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
        }

        public override int SaveChanges()
        {
            this.ModifyModelTimestamp();
            return base.SaveChanges();
        }

        private void ModifyModelTimestamp()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is BaseEntity &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                var baseEntity = (BaseEntity)entityEntry.Entity;
                baseEntity.UpdateTime = DateTime.Now;

                if (entityEntry.State == EntityState.Added)
                {
                    baseEntity.CreateTime = DateTime.Now;
                }
            }
        }
    }
}