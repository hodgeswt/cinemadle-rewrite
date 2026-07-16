using Cinemadle.Database;
using Cinemadle.Jobs;
using Microsoft.AspNetCore.Identity;
using Moq;
using Quartz;

namespace Cinemadle.UnitTest;

public class EmailAnonymizationUnitTests
{
    [Theory]
    [InlineData("test@example.com", "973dfe463ec85785f5f95af5ba3906eedb2d931c24e69824a89ea65dba4e813b")]
    [InlineData("example@example.com", "31c5543c1734d25c7206f5fd591525d0295bec6fe84ff82f946a34fe970a1e66")]
    [InlineData("", "")]
    [InlineData(null, "")]
    public void EmailAnonymizationTest(string? email, string expected)
    {
        var result = EmailAnonymizationJob.Anonymize(email);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(new[] {
        "test@example.com", "example@example.com", ""
    }, new[] {
        "973dfe463ec85785f5f95af5ba3906eedb2d931c24e69824a89ea65dba4e813b", "31c5543c1734d25c7206f5fd591525d0295bec6fe84ff82f946a34fe970a1e66", ""
    })]
    public void DbAnonymizationTest(string[] emails, string[] anonymizedEmails)
    {
        using var db = Mocks.GetDatabaseContext();
        using var identityDb = Mocks.GetIdentityContext();

        var users = emails.Select(email => new IdentityUser { Email = email, UserName = email, NormalizedEmail = email, NormalizedUserName = email }).ToList();
        identityDb.Users.AddRange(users);
        identityDb.SaveChanges();
        
        Assert.Equivalent(emails, identityDb.Users.Select(u => u.Email));
        
        var logger = UnitTestAssist.GetLogger<EmailAnonymizationJob>();
        
        var job = new EmailAnonymizationJob(identityDb, db, logger);

        job.Execute(Mock.Of<IJobExecutionContext>()).Wait();

        var jobStatus = db.OneTimeJobs.Where(x => x.JobName == EmailAnonymizationJob.JobName).Select(x => x.Completed).FirstOrDefault();
        
        Assert.Equivalent(anonymizedEmails, identityDb.Users.Select(u => u.Email));
        Assert.Equivalent(anonymizedEmails, identityDb.Users.Select(u => u.NormalizedEmail));
        Assert.Equivalent(anonymizedEmails, identityDb.Users.Select(u => u.UserName));
        Assert.Equivalent(anonymizedEmails, identityDb.Users.Select(u => u.NormalizedUserName));
        Assert.True(jobStatus);
    }
    
    [Fact]
    public void DbAnonymizationTestDoesNotRunWhenAlreadyCompleted()
    {
        using var db = Mocks.GetDatabaseContext();
        using var identityDb = Mocks.GetIdentityContext();
        
        const string testEmail = "test@test.com";
        
        identityDb.Users.Add(new IdentityUser { Email = testEmail, UserName = testEmail, NormalizedEmail = testEmail, NormalizedUserName = testEmail });
        identityDb.SaveChanges();
        
        Assert.Single(identityDb.Users);
        
        db.OneTimeJobs.Add(new OneTimeJob { JobName = EmailAnonymizationJob.JobName, Completed = true });
        db.SaveChanges();
        
        Assert.Single(db.OneTimeJobs);
        
        var logger = UnitTestAssist.GetLogger<EmailAnonymizationJob>();
        
        var job = new EmailAnonymizationJob(identityDb, db, logger);
        
        job.Execute(Mock.Of<IJobExecutionContext>()).Wait();
        
        Assert.Equal(testEmail, identityDb.Users.Single().Email);
        Assert.Equal(testEmail, identityDb.Users.Single().NormalizedEmail);
        Assert.Equal(testEmail, identityDb.Users.Single().UserName);
        Assert.Equal(testEmail, identityDb.Users.Single().NormalizedUserName);
    }
}