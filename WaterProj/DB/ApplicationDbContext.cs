using Microsoft.EntityFrameworkCore;
using WaterProj.Models;

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
        public DbSet<Stop>  Stop { get; set; }

    }
}
