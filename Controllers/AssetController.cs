using API_SVsharp.DTO.Asset;
using API_SVsharp.Application.Interfaces;
using API_SVsharp.DTO.Response;
using Microsoft.AspNetCore.Mvc;

namespace API_SVsharp.Controllers
{
    [Route("api/assets")]
    [ApiController]
    public class AssetController : ControllerBase
    {
        private readonly IAssetService _assetService;

        public AssetController(IAssetService assetService)
        {
            _assetService = assetService;
        }

        // ==============================
        // GET /api/assets
        // ==============================
        [HttpGet]
        public async Task<ActionResult<ResponseModel<List<AssetResponseDTO>>>> Listar()
        {
            var response = await _assetService.ListarAssets();
            return response.Status ? Ok(response) : BadRequest(response);
        }

        // ==============================
        // GET /api/assets/{id}
        // ==============================
        [HttpGet("{id:int}")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> BuscarPorId(int id)
        {
            var response = await _assetService.BuscarAssetPorId(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // ==============================
        // POST /api/assets
        // ==============================
        [HttpPost]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> Criar([FromBody] AssetCriacaoDTO dto)
        {
            var response = await _assetService.CriarAsset(dto);

            if (!response.Status)
                return BadRequest(response);

            return CreatedAtAction(nameof(BuscarPorId), new { id = response.Dados?.Id }, response);
        }

        // ==============================
        // PUT /api/assets/{id}
        // ==============================
        [HttpPut("{id:int}")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> Editar(int id, [FromBody] EditarAssetDTO dto)
        {
            var response = await _assetService.EditarAsset(id, dto);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // ==============================
        // PATCH /api/assets/{id}/archive
        // ==============================
        [HttpPatch("{id:int}/archive")]
        public async Task<ActionResult<ResponseModel<bool>>> Arquivar(int id)
        {
            var response = await _assetService.ArquivarAsset(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // ==============================
        // PATCH /api/assets/{id}/restore
        // ==============================
        [HttpPatch("{id:int}/restore")]
        public async Task<ActionResult<ResponseModel<bool>>> Restaurar(int id)
        {
            var response = await _assetService.RestaurarAsset(id);
            return response.Status ? Ok(response) : NotFound(response);
        }

        // ==============================
        // POST /api/assets/{id}/vulns/{vulnId}
        // ==============================
        [HttpPost("{id:int}/vulns/{vulnId:int}")]
        public async Task<ActionResult<ResponseModel<AssetResponseDTO>>> AdicionarVuln(int id, int vulnId)
        {
            var response = await _assetService.AdicionarVulnAoAsset(id, vulnId);
            return response.Status ? Ok(response) : BadRequest(response);
        }
    }
}