namespace MobyPark.Services.Results;

public enum ServiceStatus
{
    Success,
    Fail,
    NotFound,
    BadRequest,
    Exception
}

public class ServiceResult<T>
{
    public string? Error { get; set; }
    public T? Data { get; set; }
    
    public ServiceStatus Status { get; set; }

    public static ServiceResult<T> Ok(T data) => new ServiceResult<T> { Status = ServiceStatus.Success, Data = data };
    public static ServiceResult<T> Fail(string error) => new ServiceResult<T> { Status = ServiceStatus.Fail, Error = error };
    
    public static ServiceResult<T> NotFound(string error) => new ServiceResult<T> { Status = ServiceStatus.NotFound, Error = error };

    public static ServiceResult<T> BadRequest(string error) => new ServiceResult<T> { Status = ServiceStatus.BadRequest, Error = error };
    
    public static ServiceResult<T> Exception(string error) => new ServiceResult<T> { Status = ServiceStatus.Exception, Error = error };
    
}

// public record ServiceResult<T>
// (
//     ServiceStatus Status,
//     string? Error = null,
//     T? Data = default
// )
// {
//     public static ServiceResult<T> Ok(T data) => new(ServiceStatus.Success, null, data);
//
//     public static ServiceResult<T> Fail(string error) => new(ServiceStatus.Fail, error, default);
//
//     public static ServiceResult<T> NotFound(string error) => new(ServiceStatus.NotFound, error, default);
//
//     public static ServiceResult<T> BadRequest(string error) => new(ServiceStatus.BadRequest, error, default);
//
//     public static ServiceResult<T> Exception(string error) => new(ServiceStatus.Exception, error, default);
// }