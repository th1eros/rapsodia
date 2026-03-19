using API_SVsharp.DTO.Vuln;
using API_SVsharp.Application.Interfaces;
using Microsoft.AspNetCore.Mvc;
using API_SVsharp.DTO.Response;
using Microsoft.AspNetCore.Authorization;

namespace API_SVsharp.Controllers
{
    [Route("api/vulns")]
    [ApiController]
    [Authorize] // JWT obrigatório em todas as rotas deste controller.
    public class VulnController : ControllerBase
    {
        private readonly IVulnService _vulnService;

        public VulnController(IVulnService vulnService)
        {
            _vulnService = vulnService;
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
        public async Task<ActionResult<ResponseModel<VulnResponseDTO>>> Criar([FromBody] VulnCriacaoDTO dto)
        {
            var response = await _vulnService.CriarVuln(dto);
            if (!response.Status) return BadRequest(response);
            return CreatedAtAction(nameof(BuscarPorId), new { id = response.Dados?.Id }, response);
        }

        // PUT api/vulns/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseModel<VulnResponseDTO>>> Editar(
            [FromRoute] int id, [FromBody] EditarVulnDTO dto)
        {
            var response = await _vulnService.EditarVuln(id, dto);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH api/vulns/{id}/archive
        [HttpPatch("{id}/archive")]
        public async Task<ActionResult<ResponseModel<bool>>> Arquivar([FromRoute] int id)
        {
            var response = await _vulnService.ArquivarVuln(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH api/vulns/{id}/restore
        [HttpPatch("{id}/restore")]
        public async Task<ActionResult<ResponseModel<bool>>> Restaurar([FromRoute] int id)
        {
            var response = await _vulnService.RestaurarVuln(id);
            return response.Status ? Ok(response) : NotFound(response);
        }
    }
}
