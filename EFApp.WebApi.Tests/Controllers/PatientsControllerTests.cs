
using EFApp.Application.DTOs;
using EFApp.Application.Exceptions;
using EFApp.Application.Interfaces;
using EFApp.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace EFApp.WebApi.Tests.Controllers
{
    public class PatientsControllerTests
    {
        private readonly Mock<IPatientService> _mockSvc;
        private readonly PatientsController _ctrl;

        public PatientsControllerTests()
        {
            _mockSvc = new Mock<IPatientService>();
            _ctrl    = new PatientsController(_mockSvc.Object);
        }

        [Fact]
        public async Task Get_Returns_Ok_When_Found()
        {
            var dto = new PatientDetailsDto 
            { 
                Patient = new PatientDto { IdPatient = 5, FirstName = "A", LastName = "B", Birthdate = DateTime.Today } 
            };
            _mockSvc
                .Setup(s => s.GetPatientDetailsAsync(5))
                .ReturnsAsync(dto);

            var actionResult = await _ctrl.Get(5);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Same(dto, ok.Value);

            _mockSvc.Verify(s => s.GetPatientDetailsAsync(5), Times.Once);
        }

        [Fact]
        public async Task Get_Returns_NotFound_When_Null()
        {
            _mockSvc
                .Setup(s => s.GetPatientDetailsAsync(It.IsAny<int>()))
                .ReturnsAsync((PatientDetailsDto)null);

            var actionResult = await _ctrl.Get(99);

            Assert.IsType<NotFoundResult>(actionResult.Result);
            _mockSvc.Verify(s => s.GetPatientDetailsAsync(99), Times.Once);
        }

        [Fact]
        public async Task Get_Propagates_GenericException_From_Service()
        {
            _mockSvc
                .Setup(s => s.GetPatientDetailsAsync(7))
                .ThrowsAsync(new Exception("Boom"));

            var ex = await Assert.ThrowsAsync<Exception>(() => _ctrl.Get(7));
            Assert.Equal("Boom", ex.Message);

            _mockSvc.Verify(s => s.GetPatientDetailsAsync(7), Times.Once);
        }

        [Fact]
        public async Task Get_Propagates_NotFoundException_From_Service()
        {
            _mockSvc
                .Setup(s => s.GetPatientDetailsAsync(8))
                .ThrowsAsync(new NotFoundException("Patient 8 not found"));

            var ex = await Assert.ThrowsAsync<NotFoundException>(() => _ctrl.Get(8));
            Assert.Contains("8 not found", ex.Message);

            _mockSvc.Verify(s => s.GetPatientDetailsAsync(8), Times.Once);
        }

        [Fact]
        public async Task Get_Still_Calls_Service_Even_When_ModelStateInvalid()
        {
            _ctrl.ModelState.AddModelError("dummy", "Ignored");
            _mockSvc
                .Setup(s => s.GetPatientDetailsAsync(10))
                .ReturnsAsync(new PatientDetailsDto { Patient = new PatientDto { IdPatient = 10 } });

            var actionResult = await _ctrl.Get(10);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Equal(10, ((PatientDetailsDto)ok.Value).Patient.IdPatient);

            _mockSvc.Verify(s => s.GetPatientDetailsAsync(10), Times.Once);
        }
    }
}
