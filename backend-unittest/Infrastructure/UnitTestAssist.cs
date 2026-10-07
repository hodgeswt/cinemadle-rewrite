using System.Reflection;
using Cinemadle.Mediation;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Moq;

namespace Cinemadle.UnitTest.Infrastructure;

public class ActivationException(Type t) : Exception($"Unable to activate type {t}");

public class AssemblyDiscoverHandlersProvider : IHandlersProvider
{
  public T GetHandler<T>() where T : notnull
  {
    var t = typeof(Mediator).Assembly.GetTypes().FirstOrDefault(x => x.IsAssignableTo(typeof(T))) ?? throw new ActivationException(typeof(T));
    var m = typeof(UnitTestAssist).GetMethod(nameof(UnitTestAssist.CreateInstanceWithMocks))!.MakeGenericMethod(t);
    return (T)m.Invoke(null, [Array.Empty<object>()])!;
  }
}

public abstract class UnitTestAssist
{
    private static readonly ILoggerFactory LoggerFactory = Microsoft.Extensions.Logging.LoggerFactory.Create(x => x.AddConsole());

    public static ILogger<T> GetLogger<T>()
    {
        return LoggerFactory.CreateLogger<T>();
    }

    private static readonly List<object> _sharedObjectPool = [];

    /// <summary>
    /// Create instance of type T. Type T must have unique parameters,
    /// And this will try to create mocked versions of all parameters
    /// that were not provided as inputs.
    /// </summary>
    /// <typeparam name="T">Type to instantiate</typeparam>
    /// <param name="args">Arguments to the constructor</param>
    /// <returns>Instance, if possible; otherwise, null</returns>
    public static T CreateInstanceWithMocks<T>(params object[] args) where T : class
    {
        var candidates = typeof(T).GetConstructors().Where(x => x.IsPublic);

        ConstructorInfo? ctor = null;
        Dictionary<int, int> parameterOrder = [];
        ParameterInfo[] actualParams = [];
        foreach (var candidate in candidates)
        {
            actualParams = candidate.GetParameters();
            parameterOrder = [];
            bool allMatched = true;
            for (int i = 0; i < args.Length; i++)
            {
                var param = actualParams
                    .Where(x => x.ParameterType.IsInstanceOfType(args[i]) && !parameterOrder.ContainsKey(x.Position))
                    .ToList();
                if (param.Count != 1)
                {
                    allMatched = false;
                    break;
                }

                parameterOrder.Add(param[0].Position, i);
            }

            if (!allMatched)
            {
                continue;
            }

            ctor = candidate;
            break;
        }

        if (ctor is null)
        {
            throw new ActivationException(typeof(T));
        }

        object[] finalParams = new object[actualParams.Length];

        for (int i = 0; i < actualParams.Length; i++)
        {
            if (parameterOrder.TryGetValue(i, out int t))
            {
                finalParams[i] = args[t];
                continue;
            }

            Type pt = actualParams[i].ParameterType;
            Type mockedType = typeof(Mock<>).MakeGenericType(pt);

            var mockCtor = typeof(CinemadleMocks)
                .GetMethods()
                .FirstOrDefault(x =>
                    x.ReturnType == mockedType &&
                    x.IsStatic &&
                    x.GetParameters().All(p => p.IsOptional)
                );

            if (mockCtor is not null)
            {
                var mock = InvokeWithDefaults(mockCtor) ?? throw new ActivationException(mockedType);
                finalParams[i] = (mock as Mock)?.Object ?? throw new ActivationException(mockedType);
                continue;
            }

            var stubCtor = typeof(CinemadleMocks)
                .GetMethods()
                .FirstOrDefault(x =>
                    x.ReturnType == pt &&
                    x.IsStatic &&
                    x.GetParameters().All(p => p.IsOptional)
                ) ?? throw new ActivationException(pt);

            finalParams[i] = InvokeWithDefaults(stubCtor) ?? throw new ActivationException(pt);
        }

        return Activator.CreateInstance(typeof(T), finalParams) as T ?? throw new ActivationException(typeof(T));
    }

    /// <summary>
    /// Invoke a static method, supplying default values for any optional parameters.
    /// Reflection does not fill in optional parameters automatically, so
    /// Invoke(null, null) throws TargetParameterCountException for them.
    /// </summary>
    private static object? InvokeWithDefaults(MethodInfo method)
    {
        var parameters = method.GetParameters();
        object?[] args = new object?[parameters.Length];
        for (int i = 0; i < parameters.Length; i++)
        {
            args[i] = parameters[i].HasDefaultValue ? parameters[i].DefaultValue : Type.Missing;
        }

        return method.Invoke(null, args);
    }
}

public class CinemadleWebApplicationFactoryBase(Dictionary<string, string>? configuration = null) : WebApplicationFactory<Program>
{
    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"cinemadle-integrationtest-{Guid.NewGuid()}.db");

    private static Dictionary<string, string> TestConfiguration { get; } = new()
    {
        { "DisableQuartz", "true" },
        { "CinemadleTestMode", "true" },
    };

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        const string dbConnStringKey = "DatabaseConnectionString";
        builder.UseSetting("DatabaseConnectionString", $"DataSource={_dbPath}");

        foreach (var entry in TestConfiguration)
        {
            if (entry.Key == dbConnStringKey)
            {
                throw new ArgumentException($"Configuration value {dbConnStringKey} is not allowed to be overridden");
            }
            builder.UseSetting(entry.Key, entry.Value);
        }

        if (configuration is null)
        {
            return;
        }

        foreach (var entry in configuration)
        {
            if (entry.Key == dbConnStringKey)
            {
                throw new ArgumentException($"Configuration value {dbConnStringKey} is not allowed to be overridden");
            }
            builder.UseSetting(entry.Key, entry.Value);
        }
    }
}

public class CinemadleWebApplicationFactory() : CinemadleWebApplicationFactoryBase();

public class CinemadleWebApplicationFactoryTestModeDisabled(): CinemadleWebApplicationFactoryBase(new() { { "CinemadleTestMode", "false" } });

public class FeatureFlagWebApplicationFactory() : CinemadleWebApplicationFactoryBase(
    new()
    {
        ["CinemadleConfig:FeatureFlags:TestTrue"] = "true",
        ["CinemadleConfig:FeatureFlags:TestFalse"] = "false",
    }
);