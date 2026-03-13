using API_SVsharp.DTO.Asset;
using API_SVsharp.DTO.Response;

namespace API_SVsharp.Application.Interfaces
{
    public interface IAssetService
    {
        Task<ResponseModel<List<AssetResponseDTO>>> ListarAssets();
        Task<ResponseModel<AssetResponseDTO>>       BuscarAssetPorId(int idAsset);
        Task<ResponseModel<AssetResponseDTO>>       CriarAsset(AssetCriacaoDTO dto);
        Task<ResponseModel<AssetResponseDTO>>       EditarAsset(int idAsset, EditarAssetDTO dto);
        Task<ResponseModel<bool>>                   ArquivarAsset(int idAsset);
        Task<ResponseModel<bool>>                   RestaurarAsset(int idAsset);
        Task<ResponseModel<AssetResponseDTO>>       AdicionarVulnAoAsset(int idAsset, int idVuln);
        Task<ResponseModel<bool>>                   RemoverVulnDoAssetAsync(int assetId, int vulnId);
    }
}
