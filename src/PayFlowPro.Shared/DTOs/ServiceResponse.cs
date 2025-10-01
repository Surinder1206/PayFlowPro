namespace PayFlowPro.Shared.DTOs;

/// <summary>
/// Generic service response wrapper
/// </summary>
/// <typeparam name="T">Type of data being returned</typeparam>
public class ServiceResponse<T>
{
    public bool IsSuccess { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
    public List<string> Errors { get; set; } = new();

    public static ServiceResponse<T> Success(T data, string message = "")
    {
        return new ServiceResponse<T>
        {
            IsSuccess = true,
            Message = message,
            Data = data
        };
    }

    public static ServiceResponse<T> Failure(string message, List<string>? errors = null)
    {
        return new ServiceResponse<T>
        {
            IsSuccess = false,
            Message = message,
            Data = default,
            Errors = errors ?? new List<string>()
        };
    }
}