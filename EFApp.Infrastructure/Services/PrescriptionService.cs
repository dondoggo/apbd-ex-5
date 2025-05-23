using EFApp.Application.DTOs;
using EFApp.Application.Exceptions;
using EFApp.Application.Interfaces;
using EFApp.Domain.Entities;
using EFApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace EFApp.Application.Services
{
    public class PrescriptionService : IPrescriptionService
    {
        private readonly AppDbContext _db;
        public PrescriptionService(AppDbContext db) => _db = db;

        public async Task<int> CreatePrescriptionAsync(CreatePrescriptionRequest dto)
        {
            if (dto.Medicaments == null || dto.Medicaments.Count == 0)
                throw new BadRequestException("At least one medicament required.");
            if (dto.Medicaments.Count > 10)
                throw new BadRequestException("A prescription can contain at most 10 medicaments.");
            if (dto.DueDate < dto.Date)
                throw new BadRequestException("DueDate must be on or after Date.");

            var doctor = await _db.Doctors.FindAsync(dto.IdDoctor);
            if (doctor == null)
                throw new NotFoundException($"Doctor with Id={dto.IdDoctor} not found.");

            var medIds   = dto.Medicaments.Select(m => m.IdMedicament).Distinct();
            var existing = await _db.Medicaments
                                    .Where(m => medIds.Contains(m.IdMedicament))
                                    .Select(m => m.IdMedicament)
                                    .ToListAsync();
            var missing  = medIds.Except(existing).ToList();
            if (missing.Any())
                throw new NotFoundException($"Medicament(s) not found: {string.Join(", ", missing)}");

            Patient patient;
            if (dto.Patient.IdPatient.HasValue)
            {
                patient = await _db.Patients.FindAsync(dto.Patient.IdPatient.Value)
                          ?? new Patient
                          {
                              FirstName = dto.Patient.FirstName,
                              LastName  = dto.Patient.LastName,
                              Birthdate = dto.Patient.Birthdate
                          };
                if (patient.IdPatient == 0)
                    _db.Patients.Add(patient);
            }
            else
            {
                patient = new Patient
                {
                    FirstName = dto.Patient.FirstName,
                    LastName  = dto.Patient.LastName,
                    Birthdate = dto.Patient.Birthdate
                };
                _db.Patients.Add(patient);
            }

            var prescription = new Prescription
            {
                Date                     = dto.Date,
                DueDate                  = dto.DueDate,
                Patient                  = patient,
                Doctor                   = doctor,
                PrescriptionMedicaments = dto.Medicaments.Select(pm => new PrescriptionMedicament
                {
                    IdMedicament = pm.IdMedicament,
                    Dose         = pm.Dose,
                    Details      = pm.Details
                }).ToList()
            };
            _db.Prescriptions.Add(prescription);
            prescription.RowVersion = new byte[8];

            try
            {
                await _db.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                throw new ConcurrencyException("A concurrency conflict occurred while saving the prescription.", ex);
            }

            return prescription.IdPrescription;
        }

        public async Task<PrescriptionDetailsDto> GetPrescriptionByIdAsync(int id)
        {
            var pr = await _db.Prescriptions
                .Include(p => p.Doctor)
                .Include(p => p.PrescriptionMedicaments)
                  .ThenInclude(pm => pm.Medicament)
                .FirstOrDefaultAsync(p => p.IdPrescription == id);

            if (pr == null)
                return null;

            return new PrescriptionDetailsDto
            {
                IdPrescription = pr.IdPrescription,
                Date           = pr.Date,
                DueDate        = pr.DueDate,
                Doctor = new DoctorDto
                {
                    IdDoctor  = pr.Doctor.IdDoctor,
                    FirstName = pr.Doctor.FirstName,
                    LastName  = pr.Doctor.LastName,
                    Email     = pr.Doctor.Email
                },
                Medicaments = pr.PrescriptionMedicaments.Select(pm => new MedicamentDto
                {
                    IdMedicament = pm.IdMedicament,
                    Name         = pm.Medicament.Name,
                    Dose         = pm.Dose,
                    Details      = pm.Details
                }).ToList()
            };
        }
    }
}
