using API_SVsharp.Data;
using API_SVsharp.DTO.Response;
using API_SVsharp.DTO.TelemetryDTO;
using API_SVsharp.Models.Entity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API_SVsharp.Services.Telemetries
{
    public class TelemetryService : ITelemetryService
    {
        private readonly AppDbContext _context;

        public TelemetryService(AppDbContext context)
        {
            _context = context;
        }

        private static TelemetryResponseDTO ToDTO(Models.Entity.Telemetry t) => new()
        {
            Id = t.Id,
            AgentId = t.AgentId,
            SessionId = t.SessionId,
            TargetFilePath = t.TargetFilePath,
            EntropyValue = t.EntropyValue,
            AnalysisTimestamp = t.AnalysisTimestamp,
            ProcessingTimeMs = t.ProcessingTimeMs,
            RiskLevel = t.RiskLevel,
            CreatedAt = t.CreatedAt
        };

        public async Task<ResponseModel<TelemetryResponseDTO>> CreateTelemetry(TelemetryCreateDTO dto)
        {
            var response = new ResponseModel<TelemetryResponseDTO>();
            try
            {
                var telemetry = new Models.Entity.Telemetry
                {
                    AgentId = dto.AgentId,
                    SessionId = dto.SessionId,
                    TargetFilePath = dto.TargetFilePath,
                    EntropyValue = dto.EntropyValue,
                    AnalysisTimestamp = dto.AnalysisTimestamp,
                    ProcessingTimeMs = dto.ProcessingTimeMs,
                    RiskLevel = dto.RiskLevel
                };

                _context.Telemetries.Add(telemetry);
                await _context.SaveChangesAsync();

                response.Dados = ToDTO(telemetry);
                response.Status = true;
                response.Mensagem = "Telemetria registrada com sucesso.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = $"Erro ao registrar telemetria: {ex.Message}";
            }
            return response;
        }

        public async Task<ResponseModel<List<TelemetryResponseDTO>>> GetLatestTelemetries(int count = 50)
        {
            var response = new ResponseModel<List<TelemetryResponseDTO>>();
            try
            {
                var telemetries = await _context.Telemetries
                    .OrderByDescending(t => t.AnalysisTimestamp)
                    .Take(count)
                    .ToListAsync();

                response.Dados = telemetries.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = $"Erro ao listar telemetrias: {ex.Message}";
            }
            return response;
        }

        public async Task<ResponseModel<TelemetryResponseDTO>> GetTelemetryById(int id)
        {
            var response = new ResponseModel<TelemetryResponseDTO>();
            try
            {
                var telemetry = await _context.Telemetries.FirstOrDefaultAsync(t => t.Id == id);
                if (telemetry == null)
                {
                    response.Status = false;
                    response.Mensagem = "Telemetria não encontrada.";
                    return response;
                }

                response.Dados = ToDTO(telemetry);
                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = $"Erro ao buscar telemetria: {ex.Message}";
            }
            return response;
        }
    }
}
