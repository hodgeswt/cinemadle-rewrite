using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

public class DatabaseContext : DbContext
{
    public DbSet<UserGuess> Guesses { get; set; }
    public DbSet<TargetMovie> TargetMovies { get; set; }
    public DbSet<DataOverride> DataOverrides { get; set; }
    public DbSet<AnonUser> AnonUsers { get; set; }
    public DbSet<UserGuess> AnonUserGuesses { get; set; }
    public DbSet<Clue> UserClues { get; set; }
    public DbSet<PurchaseDetails> Purchases { get; set; }
    public DbSet<UserAccount> UserAccounts { get; set; }

    public string DbPath { get; set; }

    public DatabaseContext(DbContextOptions<DatabaseContext> opts) : base(opts)
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        string path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "AppData", "cinemadle.db");
    }

    public DatabaseContext() : this(new DbContextOptions<DatabaseContext>())
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
