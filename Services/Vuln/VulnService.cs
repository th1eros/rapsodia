using API_SVsharp.Data;
using API_SVsharp.DTO.Vuln;
using API_SVsharp.DTO.Response;
using API_SVsharp.Models.Entity;
using API_SVsharp.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_SVsharp.Services.Vulns
{
    public class VulnService : IVulnService
    {
        private readonly AppDbContext _context;

        public VulnService(AppDbContext context)
        {
            _context = context;
        }

        // Converte entidade para DTO de resposta.
        private static VulnResponseDTO ToDTO(Vuln v) => new()
        {
            Id        = v.Id,
            Titulo    = v.Titulo,
            Ambiente  = v.Ambiente,
            Nivel     = v.Nivel,
            Status    = v.Status,
            CreatedAt = v.CreatedAt
        };

        // GET — lista todas as vulns ativas.
        public async Task<ResponseModel<List<VulnResponseDTO>>> ListarVulns()
        {
            var response = new ResponseModel<List<VulnResponseDTO>>();
            try
            {
                var vulns   = await _context.Vulns.ToListAsync();
                response.Dados  = vulns.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status   = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        // GET — busca por ID.
        public async Task<ResponseModel<VulnResponseDTO>> BuscarVulnPorId(int idVuln)
        {
            var response = new ResponseModel<VulnResponseDTO>();
            try
            {
                var vuln = await _context.Vulns.FirstOrDefaultAsync(v => v.Id == idVuln);
                if (vuln == null)
                {
                    response.Status   = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }
                response.Dados  = ToDTO(vuln);
                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status   = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        // POST — cria nova vulnerabilidade.
        public async Task<ResponseModel<VulnResponseDTO>> CriarVuln(VulnCriacaoDTO dto)
        {
            var response = new ResponseModel<VulnResponseDTO>();
            try
            {
                var vuln = new Vuln
                {
                    Titulo   = dto.Titulo,
                    Ambiente = dto.Ambiente,
                    Nivel    = dto.Nivel,
                    Status   = dto.Status
                };

                _context.Vulns.Add(vuln);
                await _context.SaveChangesAsync();

                response.Dados    = ToDTO(vuln);
                response.Status   = true;
                response.Mensagem = "Vulnerabilidade criada.";
            }
            catch (Exception ex)
            {
                response.Status   = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        // PUT — atualiza apenas os campos informados.
        public async Task<ResponseModel<VulnResponseDTO>> EditarVuln(int idVuln, EditarVulnDTO dto)
        {
            var response = new ResponseModel<VulnResponseDTO>();
            try
            {
                var vuln = await _context.Vulns.FirstOrDefaultAsync(v => v.Id == idVuln);
                if (vuln == null)
                {
                    response.Status   = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                if (dto.Titulo  != null) vuln.Titulo  = dto.Titulo;
                if (dto.Ambiente != null) vuln.Ambiente = dto.Ambiente.Value;
                if (dto.Nivel   != null) vuln.Nivel   = dto.Nivel.Value;
                if (dto.Status  != null) vuln.Status  = dto.Status.Value;

                await _context.SaveChangesAsync();

                response.Dados    = ToDTO(vuln);
                response.Status   = true;
                response.Mensagem = "Vulnerabilidade atualizada.";
            }
            catch (Exception ex)
            {
                response.Status   = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        // PATCH archive — soft delete via DeletedAt, consistente com Asset.
        // Correção: antes usava ArchivedAt, que NÃO está no global query filter.
        public async Task<ResponseModel<bool>> ArquivarVuln(int idVuln)
        {
            var response = new ResponseModel<bool>();
            try
            {
                var vuln = await _context.Vulns.FirstOrDefaultAsync(v => v.Id == idVuln);
                if (vuln == null)
                {
                    response.Status   = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                vuln.DeletedAt = DateTime.UtcNow; // Global query filter a omite das listagens.
                await _context.SaveChangesAsync();

                response.Dados    = true;
                response.Status   = true;
                response.Mensagem = "Vulnerabilidade arquivada.";
            }
            catch (Exception ex)
            {
                response.Status   = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        // PATCH restore — reativa uma vuln arquivada.
        public async Task<ResponseModel<bool>> RestaurarVuln(int idVuln)
        {
            var response = new ResponseModel<bool>();
            try
            {
                var vuln = await _context.Vulns
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(v => v.Id == idVuln);

                if (vuln == null)
                {
                    response.Status   = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                vuln.DeletedAt = null;
                await _context.SaveChangesAsync();

                response.Dados    = true;
                response.Status   = true;
                response.Mensagem = "Vulnerabilidade restaurada.";
            }
            catch (Exception ex)
            {
                response.Status   = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }
    }
}
