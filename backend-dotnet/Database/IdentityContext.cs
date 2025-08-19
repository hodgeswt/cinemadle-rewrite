using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class IdentityContext : IdentityDbContext<IdentityUser>
{
    public string DbPath { get; set; }

    public IdentityContext(DbContextOptions<IdentityContext> opts) : base(opts)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        string path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "AppData", "cinemadle.db");
    }

    public IdentityContext() : this(new DbContextOptions<IdentityContext>())
    {
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite($"DataSource={DbPath}");
        }
    }
}
