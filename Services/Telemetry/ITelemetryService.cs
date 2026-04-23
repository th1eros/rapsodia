using Rapsodia.DTO.Response;
using Rapsodia.DTO.TelemetryDTO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rapsodia.Services.Telemetries
{
    public interface ITelemetryService
    {
        Task<ResponseModel<TelemetryResponseDTO>> CreateTelemetry(TelemetryCreateDTO dto, string? apiKey);
        Task<ResponseModel<List<TelemetryResponseDTO>>> GetLatestTelemetries(int count = 50);
        Task<ResponseModel<TelemetryResponseDTO>> GetTelemetryById(int id);
        Task<ResponseModel<TelemetryStatsDTO>> GetStats();
        Task<ResponseModel<List<TelemetryResponseDTO>>> GetSince(DateTime? since);
        Task<ResponseModel<bool>> DeleteTelemetry(int id);
    }
};

