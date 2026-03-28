using Rapsodia.DTO.Vuln;
using Rapsodia.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Rapsodia.DTO.Response;
using Microsoft.AspNetCore.Authorization;

namespace Rapsodia.Controllers
{
    [Route("api/vulns")]
    [ApiController]
    [Authorize] // JWT obrigatÃ³rio em todas as rotas deste controller.
    public class VulnController : ControllerBase
    {
        private readonly IVulnService _vulnService;
        private readonly ILogger<VulnController> _logger;

        public VulnController(IVulnService vulnService, ILogger<VulnController> logger)
        {
            _vulnService = vulnService;
            _logger = logger;
        }

        // GET api/vulns
        [HttpGet]
        public async Task<ActionResult<ResponseModel<List<VulnResponseDTO>>>> Listar()
        {
            var response = await _vulnService.ListarVulns();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/vulns/archived
        [HttpGet("archived")]
        public async Task<ActionResult<ResponseModel<List<VulnResponseDTO>>>> ListarArquivados()
        {
            var response = await _vulnService.ListarVulnsArquivadas();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/vulns/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseModel<VulnResponseDTO>>> BuscarPorId([FromRoute] int id)
        {
            var response = await _vulnService.BuscarVulnPorId(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // POST api/vulns
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<VulnResponseDTO>>> Criar([FromBody] VulnCriacaoDTO dto)
        {
            var response = await _vulnService.CriarVuln(dto);
            if (!response.Status) return BadRequest(response);
            return CreatedAtAction(nameof(BuscarPorId), new { id = response.Dados?.Id }, response);
        }

        // PUT api/vulns/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<VulnResponseDTO>>> Editar(
            [FromRoute] int id, [FromBody] EditarVulnDTO dto)
        {
            var response = await _vulnService.EditarVuln(id, dto);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH api/vulns/{id}/archive
        [HttpPatch("{id}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<bool>>> Arquivar([FromRoute] int id)
        {
            var response = await _vulnService.ArquivarVuln(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH api/vulns/{id}/restore
        [HttpPatch("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<bool>>> Restaurar([FromRoute] int id)
        {
            var response = await _vulnService.RestaurarVuln(id);
            return response.Status ? Ok(response) : NotFound(response);
        }
    }
}
