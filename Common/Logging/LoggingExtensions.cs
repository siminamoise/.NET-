using Microsoft.Extensions.Logging;

namespace ProductApp.Common.Logging
{
    public static class LoggingExtensions
    {
        public static void LogProductCreationMetrics(
            this ILogger logger,
            ProductCreationMetrics metrics)
        {
            logger.LogInformation(
                new EventId(LogEvents.ProductCreationCompleted),
                "Product Metrics | Operation={OperationId}, Name={Name}, SKU={SKU}, Category={Category}, " +
                "Validation={Validation}ms, DB={DbSave}ms, Total={Total}ms, Success={Success}, Error={Error}",
                metrics.OperationId,
                metrics.ProductName,
                metrics.SKU,
                metrics.Category,
                metrics.ValidationDuration.TotalMilliseconds,
                metrics.DatabaseSaveDuration.TotalMilliseconds,
                metrics.TotalDuration.TotalMilliseconds,
                metrics.Success,
                metrics.ErrorReason ?? "None"
            );
        }
    }
}
