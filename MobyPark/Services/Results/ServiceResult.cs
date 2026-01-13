namespace MobyPark.Services.Results;

public enum ServiceStatus
{
    Success,
    Fail,
    NotFound,
    BadRequest,
    Exception,
    Conflict,
    Forbidden
}

public class ServiceResult<T>
{
    public string? Error { get; set; }
    public T? Data { get; set; }

    public ServiceStatus Status { get; set; }

    public static ServiceResult<T> Ok(T data) => new() { Status = ServiceStatus.Success, Data = data };
    public static ServiceResult<T> Fail(string error) => new() { Status = ServiceStatus.Fail, Error = error };

    public static ServiceResult<T> NotFound(string error) => new() { Status = ServiceStatus.NotFound, Error = error };

    public static ServiceResult<T> BadRequest(string error) => new() { Status = ServiceStatus.BadRequest, Error = error };

    public static ServiceResult<T> Exception(string error) => new() { Status = ServiceStatus.Exception, Error = error };
    public static ServiceResult<T> Conflict(string error) => new() { Status = ServiceStatus.Conflict, Error = error };
    public static ServiceResult<T> Forbidden(string error) => new() { Status = ServiceStatus.Forbidden, Error = error };

}