using EFApp.Application.DTOs;
using EFApp.Application.Exceptions;
using EFApp.Application.Interfaces;
using EFApp.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Moq;

namespace EFApp.WebApi.Tests.Controllers
{
    public class PrescriptionsControllerTests
    {
        private readonly Mock<IPrescriptionService> _mockSvc;
        private readonly PrescriptionsController _ctrl;

        public PrescriptionsControllerTests()
        {
            _mockSvc = new Mock<IPrescriptionService>();
            _ctrl    = new PrescriptionsController(_mockSvc.Object);
        }

        [Fact]
        public async Task Create_Returns_CreatedAtAction_OnSuccess()
        {
            var dto = new CreatePrescriptionRequest();
            _mockSvc
                .Setup(s => s.CreatePrescriptionAsync(dto))
                .ReturnsAsync(42);

            var result = await _ctrl.Create(dto);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(nameof(PrescriptionsController.GetById), created.ActionName);
            Assert.Null(created.ControllerName);
            Assert.Equal(42, created.RouteValues["id"]);
            Assert.Null(created.Value);

            _mockSvc.Verify(s => s.CreatePrescriptionAsync(dto), Times.Once);
        }

        [Fact]
        public async Task Create_Returns_BadRequest_When_ModelState_Invalid()
        {
            _ctrl.ModelState.AddModelError("patient", "Required");
            var dto = new CreatePrescriptionRequest();

            var result = await _ctrl.Create(dto);

            var bad   = Assert.IsType<BadRequestObjectResult>(result);
            var error = Assert.IsType<SerializableError>(bad.Value);

            Assert.True(error.ContainsKey("patient"));
            var messages = (string[])error["patient"];
            Assert.Single(messages);
            Assert.Equal("Required", messages[0]);

            _mockSvc.VerifyNoOtherCalls();
        }



        [Fact]
        public async Task Create_Propagates_NotFoundException_From_Service()
        {
            var dto = new CreatePrescriptionRequest();
            _mockSvc
                .Setup(s => s.CreatePrescriptionAsync(dto))
                .ThrowsAsync(new NotFoundException("Doctor not found"));

            await Assert.ThrowsAsync<NotFoundException>(() => _ctrl.Create(dto));
        }

        [Fact]
        public async Task Create_Propagates_BadRequestException_From_Service()
        {
            var dto = new CreatePrescriptionRequest();
            _mockSvc
                .Setup(s => s.CreatePrescriptionAsync(dto))
                .ThrowsAsync(new BadRequestException("Too many medicaments"));

            await Assert.ThrowsAsync<BadRequestException>(() => _ctrl.Create(dto));
        }

        [Fact]
        public async Task GetById_Returns_Ok_When_Found()
        {
            var detail = new PrescriptionDetailsDto { IdPrescription = 7 };
            _mockSvc
                .Setup(s => s.GetPrescriptionByIdAsync(7))
                .ReturnsAsync(detail);

            var actionResult = await _ctrl.GetById(7);

            var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
            Assert.Same(detail, ok.Value);
            _mockSvc.Verify(s => s.GetPrescriptionByIdAsync(7), Times.Once);
        }

        [Fact]
        public async Task GetById_Returns_NotFound_When_NotFound()
        {
            _mockSvc
                .Setup(s => s.GetPrescriptionByIdAsync(It.IsAny<int>()))
                .ReturnsAsync((PrescriptionDetailsDto)null);

            var actionResult = await _ctrl.GetById(123);

            Assert.IsType<NotFoundResult>(actionResult.Result);
            _mockSvc.Verify(s => s.GetPrescriptionByIdAsync(123), Times.Once);
        }

        [Fact]
        public async Task GetById_Propagates_Exception_From_Service()
        {
            _mockSvc
                .Setup(s => s.GetPrescriptionByIdAsync(1))
                .ThrowsAsync(new Exception("Unexpected"));

            await Assert.ThrowsAsync<Exception>(() => _ctrl.GetById(1));
        }
    }
}
