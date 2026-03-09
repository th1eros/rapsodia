using API_SVsharp.DTO.Asset;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.DTO.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace API_SVsharp.Controllers
{
    [ApiController]
    [Route("api/assets")]
    [AllowAnonymous]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        // GET: api/assets
        [HttpGet]
        public async Task<ActionResult<ResponseModel<List<AssetResponseDTO>>>> Listar()
        {
            var response = await _assetService.ListarAssets();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // GET: api/assets/1
        [HttpGet("{id}")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> BuscarPorId(
            [FromRoute] int id)
        {
            var response = await _assetService.BuscarAssetPorId(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // POST: api/assets
        [HttpPost]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> Criar(
            [FromBody] AssetCriacaoDTO dto)
        {
            var response = await _assetService.CriarAsset(dto);

            if (!response.Status)
                return BadRequest(response);

            return CreatedAtAction(
                nameof(BuscarPorId),
                new { id = response.Dados?.Id },
                response);
        }

        // PUT: api/assets/1
        [HttpPut("{id}")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> Editar(
            [FromRoute] int id,
            [FromBody] EditarAssetDTO dto)
        {
            var response = await _assetService.EditarAsset(id, dto);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH: api/assets/1/archive
        [HttpPatch("{id}/archive")]
        public async Task<ActionResult<ResponseModel<bool>>> Arquivar(
            [FromRoute] int id)
        {
            var response = await _assetService.ArquivarAsset(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // PATCH: api/assets/1/restore
        [HttpPatch("{id}/restore")]
        public async Task<ActionResult<ResponseModel<bool>>> Restaurar(
            [FromRoute] int id)
        {
            var response = await _assetService.RestaurarAsset(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // POST: api/assets/1/vulnerabilities/5
        [HttpPost("{id}/vulnerabilities/{vulnId}")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> AdicionarVuln(
            [FromRoute] int id,
            [FromRoute] int vulnId)
        {
            var response = await _assetService.AdicionarVulnAoAsset(id, vulnId);
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // DELETE: api/assets/1/vulnerabilities/5
        [HttpDelete("{id}/vulnerabilities/{vulnId}")]
        public async Task<ActionResult<ResponseModel<bool>>> RemoverVulnDoAsset(
            [FromRoute] int id,
            [FromRoute] int vulnId)
        {
            var response = await _assetService.RemoverVulnDoAssetAsync(id, vulnId);
            return response.Status ? Ok(response) : NotFound(response);
        }
    }
}