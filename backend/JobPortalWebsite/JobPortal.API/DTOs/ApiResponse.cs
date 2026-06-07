namespace JobPortal.API.DTOs;

public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public T? Data { get; set; }
}

//DTOs are Data Transfer Objects.
//They are used to transfer data between the client and the server.
//Models is shape for database rows.
