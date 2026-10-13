namespace DevopsCommander.Shared;

/// <summary>
/// A generic result wrapper for functional error handling.
/// Every service method returns <c>Result&lt;T&gt;</c> instead of throwing exceptions,
/// making success/failure explicit and composable.
/// </summary>
/// <typeparam name="T">The type of the value on success.</typeparam>
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

    /// <summary>True if the operation succeeded and <see cref="Value"/> is available.</summary>
    public bool IsSuccess { get; }

    /// <summary>True if the operation failed and <see cref="Error"/> is available.</summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// The success value. Throws if accessed on a failed result.
    /// </summary>
    public T Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException($"Cannot access Value on a failed Result. Error: {_error}");

    /// <summary>
    /// The error message. Throws if accessed on a successful result.
    /// </summary>
    public string Error => IsFailure
        ? _error!
        : throw new InvalidOperationException("Cannot access Error on a successful Result.");

    // ── Factory methods ──────────────────────────────────────────────

    /// <summary>Creates a successful result wrapping the given value.</summary>
    public static Result<T> Success(T value) => new(value);

    /// <summary>Creates a failed result with the given error message.</summary>
    public static Result<T> Failure(string error) => new(error);

    // ── Implicit conversions for ergonomic usage ─────────────────────

    /// <summary>Implicitly wrap a value into a successful result.</summary>
    public static implicit operator Result<T>(T value) => Success(value);

    // ── Functional combinators ───────────────────────────────────────

    /// <summary>
    /// Transforms the value if this result is successful.
    /// If this result is a failure, the error is propagated unchanged.
    /// </summary>
    public Result<TOut> Map<TOut>(Func<T, TOut> mapper) =>
        IsSuccess ? Result<TOut>.Success(mapper(_value!)) : Result<TOut>.Failure(_error!);

    /// <summary>
    /// Chains another result-producing operation if this result is successful.
    /// If this result is a failure, the error is propagated unchanged.
    /// </summary>
    public Result<TOut> Bind<TOut>(Func<T, Result<TOut>> binder) =>
        IsSuccess ? binder(_value!) : Result<TOut>.Failure(_error!);

    /// <summary>
    /// Executes <paramref name="onSuccess"/> or <paramref name="onFailure"/>
    /// depending on the result state.
    /// </summary>
    public TOut Match<TOut>(Func<T, TOut> onSuccess, Func<string, TOut> onFailure) =>
        IsSuccess ? onSuccess(_value!) : onFailure(_error!);

    public override string ToString() =>
        IsSuccess ? $"Success({_value})" : $"Failure({_error})";
}
