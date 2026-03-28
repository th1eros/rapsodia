using Rapsodia.DTO.Asset;
using Rapsodia.Application.Interfaces;
using Rapsodia.DTO.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace Rapsodia.Controllers
{
    [ApiController]
    [Route("api/assets")]
    [Authorize] // JWT obrigatÃ³rio em todas as rotas deste controller.
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;
        private readonly ILogger<AssetController> _logger;

        public AssetController(IAssetService assetService, ILogger<AssetController> logger)
        {
            _assetService = assetService;
            _logger = logger;
        }

        // GET api/assets
        [HttpGet]
        public async Task<ActionResult<ResponseModel<List<AssetResponseDTO>>>> Listar()
        {
            var response = await _assetService.ListarAssets();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/assets/archived
        [HttpGet("archived")]
        public async Task<ActionResult<ResponseModel<List<AssetResponseDTO>>>> ListarArquivados()
        {
            var response = await _assetService.ListarAssetsArquivados();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET api/assets/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> BuscarPorId([FromRoute] int id)
        {
            var response = await _assetService.BuscarAssetPorId(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // POST api/assets
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> Criar([FromBody] AssetCriacaoDTO dto)
        {
            var response = await _assetService.CriarAsset(dto);
            if (!response.Status) return BadRequest(response);
            return CreatedAtAction(nameof(BuscarPorId), new { id = response.Dados?.Id }, response);
        }

        // PUT api/assets/{id}
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> Editar(
            [FromRoute] int id, [FromBody] EditarAssetDTO dto)
        {
            var response = await _assetService.EditarAsset(id, dto);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH api/assets/{id}/archive
        [HttpPatch("{id}/archive")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<bool>>> Arquivar([FromRoute] int id)
        {
            var response = await _assetService.ArquivarAsset(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH api/assets/{id}/restore
        [HttpPatch("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<bool>>> Restaurar([FromRoute] int id)
        {
            var response = await _assetService.RestaurarAsset(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // POST api/assets/{id}/vulns/{vulnId}
        [HttpPost("{id}/vulns/{vulnId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> AdicionarVuln(
            [FromRoute] int id, [FromRoute] int vulnId)
        {
            var response = await _assetService.AdicionarVulnAoAsset(id, vulnId);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // DELETE api/assets/{id}/vulns/{vulnId}
        [HttpDelete("{id}/vulns/{vulnId}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ResponseModel<bool>>> RemoverVulnDoAsset(
            [FromRoute] int id, [FromRoute] int vulnId)
        {
            var response = await _assetService.RemoverVulnDoAssetAsync(id, vulnId);
            return response.Status ? Ok(response) : NotFound(response);
        }
    }
}
