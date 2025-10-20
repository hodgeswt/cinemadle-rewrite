using Cinemadle.Database;

using Quartz;

namespace Cinemadle.Jobs;

public class CustomGameRemovalJob(IServiceProvider serviceProvider) : IJob
{
    private readonly string _key = "CustomGameRemovalJob";
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    public Task Execute(IJobExecutionContext context)
    {
        ILogger<CustomGameRemovalJob>? logger = null;
        try
        {
            using var scope = _serviceProvider.CreateScope();

            logger = scope.ServiceProvider.GetRequiredService<ILogger<CustomGameRemovalJob>>();
            DatabaseContext dbContext = scope.ServiceProvider.GetRequiredService<DatabaseContext>();

            DateTime cutoffDate = DateTime.UtcNow.AddDays(-2);
            IEnumerable<CustomGame> oldGames = dbContext.CustomGames.Where(g => g.Inserted < cutoffDate);

            dbContext.CustomGames.RemoveRange(oldGames);

            Statistic? existingStat = dbContext.Statistics.FirstOrDefault(s => s.Key == _key);
            if (existingStat != null)
            {
                existingStat.Count += oldGames.Count();
                existingStat.LastUpdated = DateTime.UtcNow;
                return dbContext.SaveChangesAsync();
            }
            else
            {
                dbContext.Statistics.Add(new Statistic
                {
                    Key = _key,
                    Count = oldGames.Count(),
                    LastUpdated = DateTime.UtcNow
                });
            }
            
            return dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            logger?.LogError("Error in CustomGameRemovalJob: {message}", ex.Message);
            return Task.CompletedTask;
        }
    }
}