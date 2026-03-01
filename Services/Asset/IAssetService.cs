using System.Collections.Generic;
using System.Threading.Tasks;
using API_SVsharp.DTO.Asset;
using API_SVsharp.DTO.Response;

namespace API_SVsharp.Application.Interfaces
{
    public interface IAssetService
    {
        Task<ResponseModel<List<AssetResponseDTO>>> ListarAssets();

        Task<ResponseModel<AssetResponseDTO>> BuscarAssetPorId(int idAsset);

        Task<ResponseModel<AssetResponseDTO>> AdicionarVulnAoAsset(int idAsset, int idVuln);

        Task<ResponseModel<AssetResponseDTO>> CriarAsset(AssetCriacaoDTO assetCriacaoDTO);

        Task<ResponseModel<AssetResponseDTO>> EditarAsset(int idAsset, EditarAssetDTO editarAssetDTO);

        Task<ResponseModel<bool>> ArquivarAsset(int idAsset);

        Task<ResponseModel<bool>> RestaurarAsset(int idAsset);

        Task<ResponseModel<bool>> RemoverVulnDoAssetAsync(int assetId, int vulnId);

    }
}