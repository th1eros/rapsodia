using Rapsodia.DTO.Asset;
using Rapsodia.DTO.Response;

namespace Rapsodia.Application.Interfaces
{
    public interface IAssetService
    {
        Task<ResponseModel<List<AssetResponseDTO>>> ListarAssets();
        Task<ResponseModel<List<AssetResponseDTO>>> ListarAssetsArquivados();
        Task<ResponseModel<AssetResponseDTO>> BuscarAssetPorId(int id);
        Task<ResponseModel<AssetResponseDTO>>       CriarAsset(AssetCriacaoDTO dto);
        Task<ResponseModel<AssetResponseDTO>>       EditarAsset(int idAsset, EditarAssetDTO dto);
        Task<ResponseModel<bool>>                   ArquivarAsset(int idAsset);
        Task<ResponseModel<bool>>                   RestaurarAsset(int idAsset);
        Task<ResponseModel<AssetResponseDTO>>       AdicionarVulnAoAsset(int idAsset, int idVuln);
        Task<ResponseModel<bool>>                   RemoverVulnDoAssetAsync(int assetId, int vulnId);
    }
}
