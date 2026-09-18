namespace CommunalService.Domain;

public class ApiResponse 
{ public int Code { get; set; }
    /// <summary>消息内容。</summary>
    public string Message { get; set; }
    /// <summary>响应数据体。</summary>
    public object Data { get; set; }
}

