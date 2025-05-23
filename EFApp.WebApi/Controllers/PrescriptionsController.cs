using EFApp.Application.DTOs;
using EFApp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace EFApp.WebApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PrescriptionsController : ControllerBase
    {
        private readonly IPrescriptionService _svc;
        public PrescriptionsController(IPrescriptionService svc) => _svc = svc;

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreatePrescriptionRequest dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var id = await _svc.CreatePrescriptionAsync(dto);
            return CreatedAtAction(nameof(GetById), new { id }, null);
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<PrescriptionDetailsDto>> GetById(int id)
        {
            var pres = await _svc.GetPrescriptionByIdAsync(id);
            return pres is null ? NotFound() : Ok(pres);
        }
    }
}