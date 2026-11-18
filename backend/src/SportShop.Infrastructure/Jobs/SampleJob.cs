using Microsoft.Extensions.Logging;

namespace SportShop.Infrastructure.Jobs;

public interface ISampleJob
{
    void Execute();
}

public sealed partial class SampleJob : ISampleJob
{
    private readonly ILogger<SampleJob> _logger;

    public SampleJob(ILogger<SampleJob> logger)
    {
        _logger = logger;
    }

    public void Execute()
    {
        LogSampleJobExecuted(_logger, DateTime.UtcNow);
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Sample Hangfire job executed successfully at {UtcTime}")]
    private static partial void LogSampleJobExecuted(ILogger logger, DateTime utcTime);
}
