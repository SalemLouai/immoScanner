namespace ImmoScorer.Domain.Common;

/// <summary>
/// Represents the outcome of an operation that can succeed or fail without throwing exceptions.
/// </summary>
/// <typeparam name="T">The value type returned on success.</typeparam>
public sealed class Result<T>
{
    private readonly T? _value;
    private readonly string? _error;

    private Result(T value)
    {
        _value = value;
        IsSuccess = true;
    }

    private Result(string error)
    {
        _error = error;
        IsSuccess = false;
    }

    /// <summary>Gets a value indicating whether the result represents a successful outcome.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the result represents a failure.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the value. Throws if the result is a failure.</summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("Cannot access Value of a failed Result.");

    /// <summary>Gets the error message. Returns null if the result is successful.</summary>
    public string? Error => _error;

    /// <summary>Creates a successful result containing <paramref name="value"/>.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failed result with <paramref name="error"/> as the error message.</summary>
    public static Result<T> Failure(string error) => new(error);
}

/// <summary>
/// Non-generic result for operations that return no value.
/// </summary>
public sealed class Result
{
    private readonly string? _error;

    private Result() => IsSuccess = true;

    private Result(string error)
    {
        _error = error;
        IsSuccess = false;
    }

    /// <summary>Gets a value indicating whether the result represents a successful outcome.</summary>
    public bool IsSuccess { get; }

    /// <summary>Gets a value indicating whether the result represents a failure.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>Gets the error message. Returns null if the result is successful.</summary>
    public string? Error => _error;

    /// <summary>Creates a successful result.</summary>
    public static Result Success() => new();

    /// <summary>Creates a failed result with <paramref name="error"/> as the error message.</summary>
    public static Result Failure(string error) => new(error);
}
