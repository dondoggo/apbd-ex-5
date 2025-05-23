using EFApp.Application.DTOs;
using EFApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EFApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PatientsController : ControllerBase
    {
        private readonly IPatientService _svc;
        public PatientsController(IPatientService svc) => _svc = svc;

        [HttpGet("{id}")]
        public async Task<ActionResult<PatientDetailsDto>> Get(int id)
        {
            var res = await _svc.GetPatientDetailsAsync(id);
            return res is null ? NotFound() : Ok(res);
        }
    }
}