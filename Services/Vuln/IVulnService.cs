using Rapsodia.DTO.Vuln;
using Rapsodia.DTO.Response;

namespace Rapsodia.Application.Interfaces
{
    public interface IVulnService
    {
        Task<ResponseModel<List<VulnResponseDTO>>> ListarVulns();
        Task<ResponseModel<List<VulnResponseDTO>>> ListarVulnsArquivadas();
        Task<ResponseModel<VulnResponseDTO>> BuscarVulnPorId(int id);
        Task<ResponseModel<VulnResponseDTO>>       CriarVuln(VulnCriacaoDTO dto);
        Task<ResponseModel<VulnResponseDTO>>       EditarVuln(int idVuln, EditarVulnDTO dto);
        Task<ResponseModel<bool>>                  ArquivarVuln(int idVuln);
        Task<ResponseModel<bool>>                  RestaurarVuln(int idVuln);
    }
}
