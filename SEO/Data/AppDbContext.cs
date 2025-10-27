using Microsoft.EntityFrameworkCore;
using SEO.Models;

namespace SEO.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<SEOText> SEOTexts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // additional model configuration ???
            //modelBuilder.Entity<SEOText>().ToTable("SEOTexts");

        }

    }
}
