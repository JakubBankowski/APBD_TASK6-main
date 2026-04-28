namespace APBD_TASK6.Validation;

public class ValidationResult
{
    public bool IsSuccess { get; set; }
    public string? ErrorMessage { get; set; }
    public int StatusCode  { get; set; }
    
    public static ValidationResult Success() => new() { IsSuccess = true };

    public static ValidationResult Failure(string msg, int code) =>
        new() { IsSuccess = false, ErrorMessage = msg,  StatusCode = code };
}