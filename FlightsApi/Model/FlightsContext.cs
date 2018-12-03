using Microsoft.EntityFrameworkCore;

namespace FlightsApi.Model
{
    public class DatabaseContext
        : DbContext
    {
        public DatabaseContext(DbContextOptions<DatabaseContext> options) : base(options)
        {
        }

        public DbSet<Flight> Flights { get; set; }
        public DbSet<Passenger> Passengers { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Passenger>()
                .HasOne(p => p.Flight)
                .WithMany(b => b.Passengers);
        }
    }
}