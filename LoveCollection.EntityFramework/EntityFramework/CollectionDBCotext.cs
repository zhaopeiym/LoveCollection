using LoveCollection.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace LoveCollection.EntityFramework.EntityFramework
{
    public class CollectionDBCotext : DbContext
    {
        public CollectionDBCotext(DbContextOptions<CollectionDBCotext> options) : base(options)
        {
        }

        private string _connection;
        public CollectionDBCotext(string connection) => _connection = connection;

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!string.IsNullOrWhiteSpace(_connection))
                optionsBuilder.UseMySql(_connection);

        }

        public DbSet<User> Users { get; set; }
        public DbSet<Collection> Collections { get; set; }
        public DbSet<Tag> Tags { get; set; }
        public DbSet<Core.Entities.Type> Types { get; set; }
    }
}
