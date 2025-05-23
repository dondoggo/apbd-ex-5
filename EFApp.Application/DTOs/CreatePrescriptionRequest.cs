using System.ComponentModel.DataAnnotations;

namespace EFApp.Application.DTOs {
public class CreatePrescriptionRequest
    {
        [Required]
        public PatientDto Patient { get; set; }

        [Range(1, int.MaxValue)]
        public int IdDoctor { get; set; }

        [DataType(DataType.DateTime)]
        public DateTime Date { get; set; }

        [DataType(DataType.DateTime)]
        [CustomValidation(typeof(ValidationHelpers), nameof(ValidationHelpers.ValidateDueDate))]
        public DateTime DueDate { get; set; }

        [MinLength(1), MaxLength(10)]
        public List<PrescriptionMedicamentDto> Medicaments { get; set; }
    }
    public static class ValidationHelpers
    {
        public static ValidationResult ValidateDueDate(DateTime due, ValidationContext ctx)
        {
            var dto = (CreatePrescriptionRequest)ctx.ObjectInstance;
            return due >= dto.Date
                ? ValidationResult.Success
                : new ValidationResult("DueDate must be >= Date");
        }
    }
    public class PatientDto
    {
        public int? IdPatient { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime Birthdate { get; set; }
    }

    public class PrescriptionMedicamentDto
    {
        public int IdMedicament { get; set; }
        public int Dose { get; set; }
        public string Details { get; set; }
    }

    public class PrescriptionDetailsDto
    {
        public int IdPrescription { get; set; }
        public DateTime Date { get; set; }
        public DateTime DueDate { get; set; }
        public DoctorDto Doctor { get; set; }
        public List<MedicamentDto> Medicaments { get; set; }
    }

    public class PatientDetailsDto
    {
        public PatientDto Patient { get; set; }
        public List<PrescriptionDetailsDto> Prescriptions { get; set; }
    }

    public class DoctorDto
    {
        public int IdDoctor { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class MedicamentDto
    {
        public int IdMedicament { get; set; }
        public string Name { get; set; }
        public int Dose { get; set; }
        public string Details { get; set; }
    }
}