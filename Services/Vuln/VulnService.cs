using API_SVsharp.Data;
using API_SVsharp.DTO.Vuln;
using API_SVsharp.DTO.Response;
using API_SVsharp.Models.Entity;
using API_SVsharp.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API_SVsharp.Services.Vulns
{
    public class VulnService : IVulnService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<VulnService> _logger;

        public VulnService(AppDbContext context, ILogger<VulnService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static VulnResponseDTO ToDTO(Vuln v) => new()
        {
            Id = v.Id,
            Titulo = v.Titulo,
            Ambiente = v.Ambiente,
            Nivel = v.Nivel,
            Status = v.Status,
            CreatedAt = v.CreatedAt
        };

        public async Task<ResponseModel<List<VulnResponseDTO>>> ListarVulns()
        {
            var response = new ResponseModel<List<VulnResponseDTO>>();
            try
            {
                var vulns = await _context.Vulns.Where(v => v.ArchivedAt == null).ToListAsync();
                response.Dados = vulns.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar vulnerabilidades ativas.");
                response.Status = false;
                response.Mensagem = "Erro ao recuperar lista de vulnerabilidades.";
            }
            return response;
        }

        public async Task<ResponseModel<List<VulnResponseDTO>>> ListarVulnsArquivadas()
        {
            var response = new ResponseModel<List<VulnResponseDTO>>();
            try
            {
                var vulns = await _context.Vulns.Where(v => v.ArchivedAt != null).ToListAsync();
                response.Dados = vulns.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar vulnerabilidades arquivadas.");
                response.Status = false;
                response.Mensagem = "Erro ao recuperar arquivo de vulnerabilidades.";
            }
            return response;
        }

        public async Task<ResponseModel<VulnResponseDTO>> BuscarVulnPorId(int idVuln)
        {
            var response = new ResponseModel<VulnResponseDTO>();
            try
            {
                var vuln = await _context.Vulns.FirstOrDefaultAsync(v => v.Id == idVuln);
                if (vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }
                response.Dados = ToDTO(vuln);
                response.Status = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar vulnerabilidade {Id}.", idVuln);
                response.Status = false;
                response.Mensagem = "Erro técnico ao localizar vulnerabilidade.";
            }
            return response;
        }

        public async Task<ResponseModel<VulnResponseDTO>> CriarVuln(VulnCriacaoDTO dto)
        {
            var response = new ResponseModel<VulnResponseDTO>();
            try
            {
                // 1. Sanitização
                var tituloSanitizado = dto.Titulo?.Trim();

                // 2. Validação
                if (string.IsNullOrWhiteSpace(tituloSanitizado))
                {
                    response.Status = false;
                    response.Mensagem = "O título da vulnerabilidade é obrigatório.";
                    return response;
                }

                var existe = await _context.Vulns.AnyAsync(v => v.Titulo.ToLower() == tituloSanitizado.ToLower());
                if (existe)
                {
                    response.Status = false;
                    response.Mensagem = $"Já existe uma vulnerabilidade registrada com o título '{tituloSanitizado}'.";
                    return response;
                }

                // 3. Persistência
                var vuln = new Vuln
                {
                    Titulo = tituloSanitizado,
                    Ambiente = dto.Ambiente,
                    Nivel = dto.Nivel,
                    Status = dto.Status,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Vulns.Add(vuln);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vulnerabilidade '{Titulo}' criada com ID {Id}.", vuln.Titulo, vuln.Id);

                response.Dados = ToDTO(vuln);
                response.Status = true;
                response.Mensagem = "Vulnerabilidade registrada com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar vulnerabilidade.");
                response.Status = false;
                response.Mensagem = "Falha ao registrar nova vulnerabilidade.";
            }
            return response;
        }

        public async Task<ResponseModel<VulnResponseDTO>> EditarVuln(int idVuln, EditarVulnDTO dto)
        {
            var response = new ResponseModel<VulnResponseDTO>();
            try
            {
                var vuln = await _context.Vulns.FirstOrDefaultAsync(v => v.Id == idVuln);
                if (vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                if (dto.Titulo != null) 
                {
                    var tituloNovo = dto.Titulo.Trim();
                    if (string.IsNullOrWhiteSpace(tituloNovo))
                    {
                        response.Status = false;
                        response.Mensagem = "O título não pode ser vazio.";
                        return response;
                    }

                    var duplicado = await _context.Vulns.AnyAsync(v => v.Titulo.ToLower() == tituloNovo.ToLower() && v.Id != idVuln);
                    if (duplicado)
                    {
                        response.Status = false;
                        response.Mensagem = "Este título já está em uso por outra vulnerabilidade.";
                        return response;
                    }
                    vuln.Titulo = tituloNovo;
                }

                if (dto.Ambiente != null) vuln.Ambiente = dto.Ambiente.Value;
                if (dto.Nivel != null) vuln.Nivel = dto.Nivel.Value;
                if (dto.Status != null) vuln.Status = dto.Status.Value;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Vulnerabilidade {Id} atualizada.", idVuln);

                response.Dados = ToDTO(vuln);
                response.Status = true;
                response.Mensagem = "Vulnerabilidade atualizada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar vulnerabilidade {Id}.", idVuln);
                response.Status = false;
                response.Mensagem = "Erro ao salvar alterações da vulnerabilidade.";
            }
            return response;
        }

        public async Task<ResponseModel<bool>> ArquivarVuln(int idVuln)
        {
            var response = new ResponseModel<bool>();
            try
            {
                var vuln = await _context.Vulns.FirstOrDefaultAsync(v => v.Id == idVuln && v.ArchivedAt == null);
                if (vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada ou já arquivada.";
                    return response;
                }

                vuln.ArchivedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Vulnerabilidade {Id} movida para o arquivo.", idVuln);

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Vulnerabilidade arquivada com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao arquivar vulnerabilidade {Id}.", idVuln);
                response.Status = false;
                response.Mensagem = "Falha técnica ao arquivar vulnerabilidade.";
            }
            return response;
        }

        public async Task<ResponseModel<bool>> RestaurarVuln(int idVuln)
        {
            var response = new ResponseModel<bool>();
            try
            {
                var vuln = await _context.Vulns
                    .FirstOrDefaultAsync(v => v.Id == idVuln && v.ArchivedAt != null);

                if (vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada no arquivo.";
                    return response;
                }

                vuln.ArchivedAt = null;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Vulnerabilidade {Id} restaurada com sucesso.", idVuln);

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Vulnerabilidade restaurada.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao restaurar vulnerabilidade {Id}.", idVuln);
                response.Status = false;
                response.Mensagem = "Erro ao processar restauração.";
            }
            return response;
        }
    }
}