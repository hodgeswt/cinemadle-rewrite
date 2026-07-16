using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

public class DatabaseContext(DbContextOptions<DatabaseContext> opts) : DbContext(opts)
{
    public DbSet<UserGuess> Guesses { get; set; }
    public DbSet<TargetMovie> TargetMovies { get; set; }
    public DbSet<DataOverride> DataOverrides { get; set; }
    public DbSet<AnonUser> AnonUsers { get; set; }
    public DbSet<UserGuess> AnonUserGuesses { get; set; }
    public DbSet<Clue> UserClues { get; set; }
    public DbSet<UserAccount> UserAccounts { get; set; }
    public DbSet<CustomGame> CustomGames { get; set; }
    public DbSet<UserHint> UserHints { get; set; }
    public DbSet<Statistic> Statistics { get; set; }
    public DbSet<OneTimeJob> OneTimeJobs { get; set; }
    public DatabaseContext() : this(new DbContextOptions<DatabaseContext>())
    {
    }

    public static string CreateDbConnectionString(string configuration)
    {
        if (!string.IsNullOrWhiteSpace(configuration)) return configuration;
        
        const Environment.SpecialFolder folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        return $"DataSource={Path.Join(path, "AppData", "cinemadle.db")}";
    }
    
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            optionsBuilder.UseSqlite(CreateDbConnectionString(string.Empty));
        }
    }
}
