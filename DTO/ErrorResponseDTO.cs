namespace APBD_TASK6.DTO;

public class ErrorResponseDTO
{
    public string Message { get; set; } = string.Empty;
    
    public ErrorResponseDTO(){}

    public ErrorResponseDTO(string message)
    {
        Message = message;
    }
}