using System.ComponentModel.DataAnnotations;

namespace APBD_TASK6.DTO;

public class UpdateAppointmentRequestDTO
{
    [Required]
    public int IdPatient { get; set; }
    [Required]
    public int IdDoctor { get; set; }
    
    public DateTime AppointmentDate { get; set; }
    
    public string Status { get; set; }
    
    [Required]
    [StringLength(250, MinimumLength = 1)]
    public string Reason { get; set; }
    public string? InternalNotes { get; set; }
}