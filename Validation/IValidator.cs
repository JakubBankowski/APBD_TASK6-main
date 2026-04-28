using APBD_TASK6.DTO;

namespace APBD_TASK6.Validation;

public interface IValidator
{
    Task<ValidationResult> ValidateCreateAsync(CreateAppointmentRequestDTO request);
    Task<ValidationResult> ValidateUpdateAsync(int id, UpdateAppointmentRequestDTO request);
    Task<ValidationResult> ValidateDeleteAsync(int id);
}