using API_SVsharp.DTO.Response;
using API_SVsharp.DTO.TelemetryDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace API_SVsharp.Services.Telemetries
{
    public interface ITelemetryService
    {
        Task<ResponseModel<TelemetryResponseDTO>> CreateTelemetry(TelemetryCreateDTO dto);
        Task<ResponseModel<List<TelemetryResponseDTO>>> GetLatestTelemetries(int count = 50);
        Task<ResponseModel<TelemetryResponseDTO>> GetTelemetryById(int id);
    }
}
