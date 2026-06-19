using FluentValidation;
using MediatR;

namespace ImmoScorer.Application.Common.Behaviors;

/// <summary>
/// MediatR pipeline behavior that runs all registered <see cref="IValidator{T}"/> validators
/// for a request before the handler executes.
/// Validation failures are returned as a failure result rather than thrown as exceptions.
/// </summary>
/// <typeparam name="TRequest">The type of the MediatR request.</typeparam>
/// <typeparam name="TResponse">The type of the MediatR response.</typeparam>
public sealed class ValidationBehavior<TRequest, TResponse>(
    IEnumerable<IValidator<TRequest>> validators)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <inheritdoc/>
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!validators.Any())
        {
            return await next();
        }

        var context = new ValidationContext<TRequest>(request);
        var validationResults = await Task.WhenAll(
            validators.Select(v => v.ValidateAsync(context, cancellationToken)));

        var failures = validationResults
            .SelectMany(r => r.Errors)
            .Where(f => f is not null)
            .ToList();

        if (failures.Count > 0)
        {
            var errorMessage = string.Join("; ", failures.Select(f => f.ErrorMessage));
            // Return a failure result via reflection to keep the Result<T> pattern intact.
            var responseType = typeof(TResponse);
            if (responseType.IsGenericType)
            {
                var genericArg = responseType.GetGenericArguments()[0];
                var failureMethod = typeof(ImmoScorer.Domain.Common.Result<>)
                    .MakeGenericType(genericArg)
                    .GetMethod(nameof(ImmoScorer.Domain.Common.Result<object>.Failure),
                        [typeof(string)])!;
                return (TResponse)failureMethod.Invoke(null, [errorMessage])!;
            }

            throw new ValidationException(failures);
        }

        return await next();
    }
}
