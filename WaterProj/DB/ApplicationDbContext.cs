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
        public DbSet<Feedback> Feedbacks { get; set; }
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

        }
    }
}
