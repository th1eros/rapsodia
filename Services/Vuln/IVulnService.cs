using System.Collections.Generic;
using System.Threading.Tasks;
using API_SVsharp.DTO.Vuln;
using API_SVsharp.DTO.Response;

namespace API_SVsharp.Application.Interfaces
{
    public interface IVulnService
    {
        Task<ResponseModel<List<VulnResponseDTO>>> ListarVulns();

        Task<ResponseModel<VulnResponseDTO>> BuscarVulnPorId(int idVuln);

        Task<ResponseModel<VulnResponseDTO>> CriarVuln(VulnCriacaoDTO vulnCriacaoDTO);

        Task<ResponseModel<VulnResponseDTO>> EditarVuln(int idVuln, EditarVulnDTO editarVulnDTO);

        Task<ResponseModel<bool>> ArquivarVuln(int idVuln);

        Task<ResponseModel<bool>> RestaurarVuln(int idVuln);
    }
}