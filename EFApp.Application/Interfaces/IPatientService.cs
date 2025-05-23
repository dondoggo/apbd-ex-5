namespace EFApp.Application.Interfaces
{
    using DTOs;
    public interface IPatientService
    {
        Task<PatientDetailsDto> GetPatientDetailsAsync(int id);
    }
}