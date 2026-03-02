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

        // ==============================
        // LISTAR VULNS
        // ==============================
        public async Task<ResponseModel<List<VulnResponseDTO>>> ListarVulns()
        {
            var response = new ResponseModel<List<VulnResponseDTO>>();

            try
            {
                var vulns = await _context.Vulns.ToListAsync();

                response.Dados = vulns.Select(v => new VulnResponseDTO
                {
                    Id = v.Id,
                    Titulo = v.Titulo,
                    Ambiente = v.Ambiente,
                    Nivel = v.Nivel,
                    Status = v.Status,
                    CreatedAt = v.CreatedAt
                }).ToList();

                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }

            return response;
        }

        // ==============================
        // BUSCAR POR ID
        // ==============================
        public async Task<ResponseModel<VulnResponseDTO>> BuscarVulnPorId(int idVuln)
        {
            var response = new ResponseModel<VulnResponseDTO>();

            try
            {
                var vuln = await _context.Vulns
                    .FirstOrDefaultAsync(v => v.Id == idVuln);

                if (vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                response.Dados = new VulnResponseDTO
                {
                    Id = vuln.Id,
                    Titulo = vuln.Titulo,
                    Ambiente = vuln.Ambiente,
                    Nivel = vuln.Nivel,
                    Status = vuln.Status,
                    CreatedAt = vuln.CreatedAt
                };

                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }

            return response;
        }

        // ==============================
        // CRIAR
        // ==============================
        public async Task<ResponseModel<VulnResponseDTO>> CriarVuln(VulnCriacaoDTO dto)
        {
            var response = new ResponseModel<VulnResponseDTO>();

            try
            {
                var vuln = new Vuln
                {
                    Titulo = dto.Titulo!,
                    Ambiente = dto.Ambiente!,
                    Nivel = dto.Nivel!,
                    Status = dto.Status 
                };

                _context.Vulns.Add(vuln);
                await _context.SaveChangesAsync();

                response.Dados = new VulnResponseDTO
                {
                    Id = vuln.Id,
                    Titulo = vuln.Titulo,
                    Ambiente = vuln.Ambiente,
                    Nivel = vuln.Nivel,
                    Status = vuln.Status,
                    CreatedAt = vuln.CreatedAt
                };

                response.Status = true;
                response.Mensagem = "Vulnerabilidade criada com sucesso.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }

            return response;
        }

        // ==============================
        // EDITAR
        // ==============================
        public async Task<ResponseModel<VulnResponseDTO>> EditarVuln(int idVuln, EditarVulnDTO dto)
        {
            var response = new ResponseModel<VulnResponseDTO>();

            try
            {
                var vuln = await _context.Vulns
                    .FirstOrDefaultAsync(v => v.Id == idVuln);

                if (vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                vuln.Titulo = dto.Titulo ?? vuln.Titulo;
                vuln.Ambiente = dto.Ambiente ?? vuln.Ambiente;
                vuln.Nivel = dto.Nivel ?? vuln.Nivel;
                vuln.Status = dto.Status ?? vuln.Status;

                await _context.SaveChangesAsync();

                response.Dados = new VulnResponseDTO
                {
                    Id = vuln.Id,
                    Titulo = vuln.Titulo,
                    Ambiente = vuln.Ambiente,
                    Nivel = vuln.Nivel,
                    Status = vuln.Status,
                    CreatedAt = vuln.CreatedAt
                };

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

        // ==============================
        // ARQUIVAR (SOFT DELETE)
        // ==============================
        public async Task<ResponseModel<bool>> ArquivarVuln(int idVuln)
        {
            var response = new ResponseModel<bool>();

            try
            {
                var vuln = await _context.Vulns
                    .FirstOrDefaultAsync(v => v.Id == idVuln);

                if (vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                vuln.ArchivedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Vulnerabilidade arquivada.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }

            return response;
        }

        // ==============================
        // RESTAURAR
        // ==============================
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
                    response.Status = false;
                    response.Mensagem = "Vulnerabilidade não encontrada.";
                    return response;
                }

                vuln.ArchivedAt = null;

                await _context.SaveChangesAsync();

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Vulnerabilidade restaurada.";
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