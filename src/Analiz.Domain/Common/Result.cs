namespace FraudShield.TransactionAnalysis.Domain.Common;

public class Result<T>
{
    public bool IsSuccess { get; }
    public T Value { get; }
    public string Error { get; }
    public List<string> Errors { get; }

    protected Result(bool isSuccess, T value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
        Errors = new List<string>();
    }

    protected Result(bool isSuccess, T value, List<string> errors)
    {
        IsSuccess = isSuccess;
        Value = value;
        Errors = errors ?? new List<string>();
        Error = string.Join(", ", Errors);
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, error: null);
    }

    public static Result<T> Failure(string error)
    {
        return new Result<T>(false, default, error);
    }

    public static Result<T> Failure(List<string> errors)
    {
        return new Result<T>(false, default, errors);
    }

    public bool IsFailure => !IsSuccess;
}