using Microsoft.EntityFrameworkCore;

namespace Cinemadle.Database;

public class DatabaseContext : DbContext
{
    public DbSet<UserGuess> Guesses { get; set; }
    public DbSet<TargetMovie> TargetMovies { get; set; }
    public DbSet<DataOverride> DataOverrides { get; set; }

    public string DbPath { get; set; }

    public DatabaseContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        string path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "AppData", "cinemadle.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        optionsBuilder.UseSqlite($"DataSource={DbPath}");
}
