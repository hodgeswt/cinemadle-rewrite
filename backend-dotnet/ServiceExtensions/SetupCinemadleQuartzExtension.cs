using Cinemadle.Jobs;
using Quartz;

namespace Cinemadle.ServiceExtensions;

public static class SetupCinemadleQuartzExtension
{
    public static IServiceCollection SetupCinemadleQuartz(this IServiceCollection services)
    {
        services.AddQuartz(qb =>
        {
            JobKey customGameRemovalJobKey = new(nameof(CustomGameRemovalJob));
            JobKey emailAnonymizationJobKey = new(nameof(EmailAnonymizationJob));
            
            qb.AddJob<CustomGameRemovalJob>(opts => opts.WithIdentity(customGameRemovalJobKey));
            qb.AddTrigger(opts => opts
                .ForJob(customGameRemovalJobKey)
                .WithIdentity($"{nameof(CustomGameRemovalJob)}-trigger")
                .WithSimpleSchedule(x => x
                    .WithInterval(TimeSpan.FromHours(24))
                    .RepeatForever()));
            
            qb.AddJob<EmailAnonymizationJob>(opts => opts.WithIdentity(emailAnonymizationJobKey));
            qb.AddTrigger(opts => opts
                .ForJob(emailAnonymizationJobKey)
                .WithIdentity($"{nameof(EmailAnonymizationJob)}-trigger")
                .WithSimpleSchedule(x => x
                    .WithRepeatCount(0)
                ));
        });
        
        services.AddQuartzHostedService(options =>
        {
            options.WaitForJobsToComplete = true;
        });

        return services;
    }
}