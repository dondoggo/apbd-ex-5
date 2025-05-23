using EFApp.Application.Exceptions;
using EFApp.Application.Interfaces;
using EFApp.Application.DTOs;
using EFApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;


namespace EFApp.Infrastructure.Services
{
    public class PatientService : IPatientService
    {
        private readonly AppDbContext _db;
        public PatientService(AppDbContext db) => _db = db;

        public async Task<PatientDetailsDto> GetPatientDetailsAsync(int id)
        {
            var patient = await _db.Patients
                .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.Doctor)
                .Include(p => p.Prescriptions)
                .ThenInclude(pr => pr.PrescriptionMedicaments)
                .ThenInclude(pm => pm.Medicament)
                .FirstOrDefaultAsync(p => p.IdPatient == id);

            if (patient == null)
                throw new NotFoundException($"Patient with Id={id} not found.");

            return new PatientDetailsDto
            {
                Patient = new PatientDto
                {
                    IdPatient = patient.IdPatient,
                    FirstName = patient.FirstName,
                    LastName = patient.LastName,
                    Birthdate = patient.Birthdate
                },
                Prescriptions = patient.Prescriptions
                    .OrderBy(pr => pr.DueDate)
                    .Select(pr => new PrescriptionDetailsDto
                    {
                        IdPrescription = pr.IdPrescription,
                        Date = pr.Date,
                        DueDate = pr.DueDate,
                        Doctor = new DoctorDto
                        {
                            IdDoctor = pr.Doctor.IdDoctor,
                            FirstName = pr.Doctor.FirstName,
                            LastName = pr.Doctor.LastName,
                            Email = pr.Doctor.Email
                        },
                        Medicaments = pr.PrescriptionMedicaments
                            .Select(pm => new MedicamentDto
                            {
                                IdMedicament = pm.IdMedicament,
                                Name = pm.Medicament.Name,
                                Dose = pm.Dose,
                                Details = pm.Details
                            })
                            .ToList()
                    })
                    .ToList()
            };
        }
    }
}