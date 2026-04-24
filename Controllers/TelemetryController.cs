using Rapsodia.DTO.Response;
using Rapsodia.DTO.TelemetryDTO;
using Rapsodia.Services.Telemetries;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Text.Json;

namespace Rapsodia.Controllers
{
    [Route("api/telemetry")]
    [ApiController]
    [Authorize] 
    public class TelemetryController : ControllerBase
    {
        private readonly ITelemetryService _telemetryService;
        private readonly ILogger<TelemetryController> _logger;

        public TelemetryController(ITelemetryService telemetryService, ILogger<TelemetryController> logger)
        {
            _telemetryService = telemetryService;
            _logger = logger;
        }

        // GET api/telemetry
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<List<TelemetryResponseDTO>>>> List([FromQuery] int count = 50)
        {
            var response = await _telemetryService.GetLatestTelemetries(count);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/telemetry/stats 
        [HttpGet("stats")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<TelemetryStatsDTO>>> Stats()
        {
            var response = await _telemetryService.GetStats();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/telemetry/stream — MONITORAMENTO EM TEMPO REAL 
        [HttpGet("stream")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<List<TelemetryResponseDTO>>>> Stream(
            [FromQuery] string? since = null)
        {
            DateTime? sinceDate = null;
            if (!string.IsNullOrEmpty(since) && DateTime.TryParse(since, out var parsed))
                sinceDate = parsed.ToUniversalTime();

            var response = await _telemetryService.GetSince(sinceDate);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/telemetry/{id}
        [HttpGet("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<TelemetryResponseDTO>>> GetById([FromRoute] int id)
        {
            var response = await _telemetryService.GetTelemetryById(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // POST api/telemetry 
        [HttpPost]
        [AllowAnonymous]
        public async Task<ActionResult<ResponseModel<TelemetryResponseDTO>>> Create(
            [FromBody] TelemetryCreateDTO dto,
            [FromHeader(Name = "X-API-KEY")] string? apiKey)
        {
            var response = await _telemetryService.CreateTelemetry(dto, apiKey);
            if (!response.Status) return BadRequest(response);
            return CreatedAtAction(nameof(GetById), new { id = response.Dados?.Id }, response);
        }

        // DELETE api/telemetry/{id} 
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<bool>>> Delete([FromRoute] int id)
        {
            var response = await _telemetryService.DeleteTelemetry(id);
            return response.Status ? Ok(response) : NotFound(response);
        }
    }
}