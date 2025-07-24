using Microsoft.Extensions.Logging;

namespace Cinemadle.UnitTest;

public class UnitTestAssist
{
    private static ILoggerFactory _loggerFactory = LoggerFactory.Create(x => x.AddConsole());

    public static ILogger<T> GetLogger<T>()
    {
        return _loggerFactory.CreateLogger<T>();
    }
}
