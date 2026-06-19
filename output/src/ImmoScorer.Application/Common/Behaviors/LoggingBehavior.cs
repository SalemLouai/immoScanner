using MediatR;
using Microsoft.Extensions.Logging;

namespace ImmoScorer.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that logs the start and completion (or failure) of every request,
/// including elapsed time. Uses structured logging with message templates.
/// </summary>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the MediatR response.</typeparam>
public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var requestName = typeof(TRequest).Name;
        var startedAt = DateTime.UtcNow;

        logger.LogInformation("Handling request {RequestName}", requestName);

        try
        {
            var response = await next();
            var elapsed = DateTime.UtcNow - startedAt;
            logger.LogInformation(
                "Handled request {RequestName} in {ElapsedMs} ms",
                requestName,
                elapsed.TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            var elapsed = DateTime.UtcNow - startedAt;
            logger.LogError(
                ex,
                "Request {RequestName} failed after {ElapsedMs} ms",
                requestName,
                elapsed.TotalMilliseconds);
            throw;
        }
    }
}
