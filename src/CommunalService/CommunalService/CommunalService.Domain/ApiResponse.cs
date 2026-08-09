namespace CommunalService.Domain;

public class ApiResponse<T> : BaseApiResponse
{
    public T Data { get; set; }
}

public class BaseApiResponse
{
    public int Code { get; set; }
    public string Message { get; set; }
}