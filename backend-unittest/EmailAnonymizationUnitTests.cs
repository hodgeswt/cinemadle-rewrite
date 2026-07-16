using Cinemadle.Jobs;

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
        string result = EmailAnonymizationJob.Anonymize(email);
        Assert.Equal(expected, result);
    }
}