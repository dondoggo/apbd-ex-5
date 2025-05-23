namespace EFApp.Application.Interfaces
{
    using DTOs;
    public interface IPrescriptionService
    {
        Task<int> CreatePrescriptionAsync(CreatePrescriptionRequest dto);
        Task<PrescriptionDetailsDto> GetPrescriptionByIdAsync(int id);
    }
}