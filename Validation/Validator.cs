using APBD_TASK6.DTO;
using Microsoft.Data.SqlClient;

namespace APBD_TASK6.Validation;

public class Validator
{
    private readonly String _connectionString;
    public Validator(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException(
                                "Missing 'DefaultConnection' in appsettings.json.");
    }
    
    public async Task<ValidationResult> ValidateCreateAsync(CreateAppointmentRequestDTO request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        if (request.AppointmentDate < DateTime.UtcNow)
            return ValidationResult.Failure("Date must be in future.", 400);

        var participantsError = await CheckParticipantsAsync(connection, request.IdPatient, request.IdDoctor);
        if (participantsError != null) return participantsError;

        var conflictError = await CheckDoctorConflictAsync(connection, request.IdDoctor, request.AppointmentDate, null);
        if (conflictError != null) return conflictError;

        return ValidationResult.Success();
    }

    public async Task<ValidationResult> ValidateUpdateAsync(int id, UpdateAppointmentRequestDTO request)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        
        var allowedStatuses = new[] { "Scheduled", "Completed", "Cancelled" };
        if (!allowedStatuses.Contains(request.Status))
        {
            return ValidationResult.Failure("Status must be Scheduled, Completed or Cancelled.", 400);
        }
        
        const string sqlStatus = """
                                 SELECT Status, AppointMentDate
                                 FROM dbo.Appointments WHERE IdAppointment = @id
                                 """;
        await using var commandStatus = new SqlCommand(sqlStatus, connection);
        commandStatus.Parameters.AddWithValue("@Id", id);

        await using (var reader = await commandStatus.ExecuteReaderAsync())
        {
            if (!await reader.ReadAsync())
                return ValidationResult.Failure("Appointment not found.", 404);

            string currentStatus = reader.GetString(0);
            DateTime currentDate = reader.GetDateTime(1);

            if (currentStatus == "Completed" && request.AppointmentDate != currentDate)
                return ValidationResult.Failure("Cannot reschedule a completed appointment.", 400);
        }

        
        var participantsError = await CheckParticipantsAsync(connection, request.IdPatient, request.IdDoctor);
        if (participantsError != null) return participantsError;

        var conflictError = await CheckDoctorConflictAsync(connection, request.IdDoctor, request.AppointmentDate, id);
        if (conflictError != null) return conflictError;
        
        
        return ValidationResult.Success();
    }
    
    public async Task<ValidationResult> ValidateDeleteAsync(int id)
    {
        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();

        const string sql = """
                           SELECT Status FROM dbo.Appointments WHERE IdAppointment = @Id
                           """;
        await using var command = new SqlCommand(sql, connection);
        command.Parameters.AddWithValue("@Id", id);
        
        await using var reader = await command.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            return ValidationResult.Failure("Appointment not found.", 404);
        if (reader.GetString(0) == "Completed")
            return ValidationResult.Failure("Cannot delete a completed appointment.", 409);
        
        return ValidationResult.Success();
    }

    private async Task<ValidationResult?> CheckParticipantsAsync(SqlConnection connection, int patientId, int doctorId)
    {
        const string sqlActive = """
                                 SELECT 
                                     (SELECT IsActive FROM dbo.Patients WHERE IdPatient = @PId) AS PatientActive,
                                     (SELECT IsActive FROM dbo.Doctors WHERE IdDoctor = @DId) AS DoctorActive
                                 """;

        await using var commandActive = new SqlCommand(sqlActive, connection);
        commandActive.Parameters.AddWithValue("@PId", patientId);
        commandActive.Parameters.AddWithValue("@DId", doctorId);

        await using (var reader = await commandActive.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                var pActive = reader["PatientActive"];
                var dActive = reader["DoctorActive"];

                if (pActive == DBNull.Value) return ValidationResult.Failure("Patient not found.", 404);
                if (dActive == DBNull.Value) return ValidationResult.Failure("Doctor not found.", 404);

                if (!(bool)pActive) return ValidationResult.Failure("Patient is inactive.", 400);
                if (!(bool)dActive) return ValidationResult.Failure("Doctor is inactive.", 400);
            }
        }

        return null;
    }
    
    private async Task<ValidationResult?> CheckDoctorConflictAsync(SqlConnection connection, int doctorId, DateTime date, int? ignoreAppointmentId = null)
    {
        const string sqlConflict = """
                                   SELECT COUNT(*) FROM dbo.Appointments
                                   WHERE IdDoctor = @DId
                                   AND AppointmentDate = @Date
                                   AND (@IgnoreId IS NULL OR IdAppointment != @IgnoreId)
                                   """;

        await using var conflictCommand = new SqlCommand(sqlConflict, connection);
        conflictCommand.Parameters.AddWithValue("@DId", doctorId);
        conflictCommand.Parameters.AddWithValue("@Date", date);
        conflictCommand.Parameters.AddWithValue("@IgnoreId", (object?)ignoreAppointmentId ?? DBNull.Value);

        var conflictCount = (int)await conflictCommand.ExecuteScalarAsync();

        if (conflictCount > 0)
            return ValidationResult.Failure("Doctor already has an appointment at this time.", 409);

        return null;
    }
}