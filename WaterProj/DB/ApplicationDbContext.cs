using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using WaterProj.Models;
using WaterProj.Models.WaterProj.Models;

namespace WaterProj.DB
{
    public class ApplicationDbContext: DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        public DbSet<Consumer> Consumers { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<Models.Route> Routes { get; set; }
        public DbSet<Ship> Ships { get; set; }
        public DbSet<Transporter> Transporters { get; set; }
        public DbSet<RouteStop> RouteStop { get; set; }
        public DbSet<Stop> Stops { get; set; }
        public DbSet<Image> Images { get; set; }
        public DbSet<Advantage> Advantages { get;  set; }
        public DbSet<RouteRating> RouteRatings { get;  set; }
        public DbSet<RouteRatingAdvantage> RouteRatingAdvantages { get;  set; }
        public DbSet<ReviewImage> ReviewImages { get; set; }
        public DbSet<ShipImage> ShipImages { get; set; }
        public DbSet<ShipСonvenience> ShipСonveniences { get; set; }
        public DbSet<Сonvenience> Сonveniences { get; set; }
        public DbSet<ShipType> ShipTypes { get; set; }
        public DbSet<Administrator> Administrators { get; set; }


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<RouteRating>()
                .HasMany(rr => rr.ReviewImages)
                .WithOne(ri => ri.RouteRating)
                .HasForeignKey(ri => ri.RouteRatingId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteStop>()
                .HasKey(rs => rs.RouteStopId);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Route)
                .WithMany(r => r.RouteStops)
                .HasForeignKey(rs => rs.RouteId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<RouteStop>()
                .HasOne(rs => rs.Stop)
                .WithMany(s => s.RouteStops)
                .HasForeignKey(rs => rs.StopId)
                .OnDelete(DeleteBehavior.Restrict); // чтобы случайно не удалить Stop, если удаляется маршрут

            modelBuilder.Entity<Ship>()
                .HasMany(s => s.ShipImages)
                .WithOne(si => si.Ship)
                .HasForeignKey(si => si.ShipId)
                .OnDelete(DeleteBehavior.Cascade);

            // Настройка связи многие-ко-многим между Ship и Сonvenience
            modelBuilder.Entity<ShipСonvenience>()
                .HasOne(sc => sc.Ship)
                .WithMany(s => s.ShipСonveniences)
                .HasForeignKey(sc => sc.ShipId);

            modelBuilder.Entity<ShipСonvenience>()
                .HasOne(sc => sc.Сonvenience)
                .WithMany(c => c.ShipСonveniences)
                .HasForeignKey(sc => sc.СonvenienceId);

            modelBuilder.Entity<Ship>()
                .HasOne(s => s.ShipType) // У одного Ship есть один ShipType
                .WithMany(st => st.Ships) // У одного ShipType может быть много Ship
                .HasForeignKey(s => s.ShipTypeId) // Внешний ключ в таблице Ship
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Ship>()
                .HasOne(s => s.Transporter)
                .WithMany(t => t.Ships)
                .HasForeignKey(s => s.TransporterId)
                .OnDelete(DeleteBehavior.Cascade);

        }
    }
}
