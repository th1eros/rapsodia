using API_SVsharp.DTO.Response;
using API_SVsharp.DTO.TelemetryDTO;
using API_SVsharp.Services.Telemetries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_SVsharp.Controllers
{
    [Route("api/telemetry")]
    [ApiController]
    [Authorize] // JWT obrigatório para garantir integridade (CISO Requirement)
    public class TelemetryController : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;

        public TelemetryController(ITelemetryService telemetryService)
        {
            _telemetryService = telemetryService;
        }

        // GET api/telemetry
        [HttpGet]
        [AllowAnonymous] // Dashboard público para monitoramento de anomalias
        public async Task<ActionResult<ResponseModel<List<TelemetryResponseDTO>>>> List([FromQuery] int count = 50)
        {
            var response = await _telemetryService.GetLatestTelemetries(count);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/telemetry/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseModel<TelemetryResponseDTO>>> GetById([FromRoute] int id)
        {
            var response = await _telemetryService.GetTelemetryById(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // POST api/telemetry
        [HttpPost]
        [AllowAnonymous] // Permitido para agentes de rede (recomendado API Key em prod)
        public async Task<ActionResult<ResponseModel<TelemetryResponseDTO>>> Create([FromBody] TelemetryCreateDTO dto)
        {
            var response = await _telemetryService.CreateTelemetry(dto);
            if (!response.Status) return BadRequest(response);
            return CreatedAtAction(nameof(GetById), new { id = response.Dados?.Id }, response);
        }
    }
}
