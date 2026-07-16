using System.Security.Cryptography;
using System.Text;
using Cinemadle.Database;
using Quartz;

namespace Cinemadle.Jobs;

public class EmailAnonymizationJob(IdentityContext identityContext, DatabaseContext dbContext, ILogger<EmailAnonymizationJob> logger) : IJob
{
    public const string JobName = nameof(EmailAnonymizationJob);

    public static string Anonymize(string? email)
    {
        if (string.IsNullOrEmpty(email))
        {
            return string.Empty;
        }
        
        var bytes = Encoding.UTF8.GetBytes(email);
        var hash = SHA256.HashData(bytes);
        var hashedEmail = Convert.ToHexStringLower(hash);

        return hashedEmail;
    }
    
    public Task Execute(IJobExecutionContext context)
    {
        logger.LogInformation("EmailAnonymizationJob Started");

        if (dbContext.OneTimeJobs.Any(x => x.Completed && x.JobName == JobName))
        {
            logger.LogInformation("EmailAnonymizationJob is already completed");
            return Task.CompletedTask;
        }

        try
        {
            foreach (var user in identityContext.Users)
            {
                if (string.IsNullOrEmpty(user.Email))
                {
                    continue;
                }

                var anonymizedEmail = Anonymize(user.Email);
                user.Email = anonymizedEmail;
                user.NormalizedEmail = anonymizedEmail;
                user.UserName = anonymizedEmail;
                user.NormalizedUserName = anonymizedEmail;
            }

            identityContext.SaveChanges();

            dbContext.OneTimeJobs.Add(new OneTimeJob
            {
                JobName = JobName,
                Completed = true
            });
            
            dbContext.SaveChanges();

            logger.LogInformation("EmailAnonymizationJob Ended");

            return Task.CompletedTask;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occured");
            return Task.CompletedTask;
        }
    }
}