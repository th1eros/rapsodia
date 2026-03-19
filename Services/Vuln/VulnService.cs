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
                response.Status = false;
                response.Mensagem = ex.Message;
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
                response.Status = false;
                response.Mensagem = ex.Message;
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
                response.Status = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel<VulnResponseDTO>> CriarVuln(VulnCriacaoDTO dto)
        {
            var response = new ResponseModel<VulnResponseDTO>();
            try
            {
                var vuln = new Vuln
                {
                    Titulo = dto.Titulo,
                    Ambiente = dto.Ambiente,
                    Nivel = dto.Nivel,
                    Status = dto.Status,
                    CreatedAt = DateTime.UtcNow // [CISO] Garantindo timestamp de auditoria
                };

                _context.Vulns.Add(vuln);
                await _context.SaveChangesAsync();

                response.Dados = ToDTO(vuln);
                response.Status = true;
                response.Mensagem = "Vulnerabilidade criada.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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

                if (dto.Titulo != null) vuln.Titulo = dto.Titulo;
                if (dto.Ambiente != null) vuln.Ambiente = dto.Ambiente.Value;
                if (dto.Nivel != null) vuln.Nivel = dto.Nivel.Value;
                if (dto.Status != null) vuln.Status = dto.Status.Value;

                await _context.SaveChangesAsync();

                response.Dados = ToDTO(vuln);
                response.Status = true;
                response.Mensagem = "Vulnerabilidade atualizada.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Vulnerabilidade arquivada com sucesso.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Vulnerabilidade restaurada para o painel principal.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }
    }
}