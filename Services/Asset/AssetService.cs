using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API_SVsharp.Data;
using API_SVsharp.DTO.Asset;
using API_SVsharp.DTO.Vuln;
using API_SVsharp.DTO.Response;
using API_SVsharp.Models.Entity;
using API_SVsharp.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace API_SVsharp.Services.Assets
{
    public class AssetService : IAssetService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AssetService> _logger;

        public AssetService(AppDbContext context, ILogger<AssetService> logger)
        {
            _context = context;
            _logger = logger;
        }

        private static AssetResponseDTO ToDTO(Asset a) => new()
        {
            Id = a.Id,
            Nome = a.Nome,
            Tipo = a.Tipo,
            Ambiente = a.Ambiente,
            Habilitado = a.Habilitado,
            CreatedAt = a.CreatedAt,
            Vulnerabilities = a.AssetVulns?.Select(av => new VulnResponseDTO
            {
                Id = av.Vuln.Id,
                Titulo = av.Vuln.Titulo,
                Ambiente = av.Vuln.Ambiente,
                Nivel = av.Vuln.Nivel,
                Status = av.Vuln.Status,
                CreatedAt = av.Vuln.CreatedAt
            }).ToList()
        };

        public async Task<ResponseModel<List<AssetResponseDTO>>> ListarAssets()
        {
            var response = new ResponseModel<List<AssetResponseDTO>>();
            try
            {
                var assets = await _context.Assets
                    .Include(a => a.AssetVulns)
                        .ThenInclude(av => av.Vuln)
                    .Where(a => a.ArchivedAt == null)
                    .ToListAsync();
                    
                response.Dados = assets.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar assets ativos.");
                response.Status = false;
                response.Mensagem = "Erro ao recuperar lista de assets.";
            }
            return response;
        }

        public async Task<ResponseModel<List<AssetResponseDTO>>> ListarAssetsArquivados()
        {
            var response = new ResponseModel<List<AssetResponseDTO>>();
            try
            {
                var assets = await _context.Assets
                    .Include(a => a.AssetVulns)
                        .ThenInclude(av => av.Vuln)
                    .Where(a => a.ArchivedAt != null)
                    .ToListAsync();
                    
                response.Dados = assets.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao listar assets arquivados.");
                response.Status = false;
                response.Mensagem = "Erro ao recuperar arquivo de assets.";
            }
            return response;
        }

        public async Task<ResponseModel<AssetResponseDTO>> BuscarAssetPorId(int idAsset)
        {
            var response = new ResponseModel<AssetResponseDTO>();
            try
            {
                var asset = await _context.Assets
                    .Include(a => a.AssetVulns)
                        .ThenInclude(av => av.Vuln)
                    .FirstOrDefaultAsync(a => a.Id == idAsset);
                    
                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado.";
                    return response;
                }
                response.Dados = ToDTO(asset);
                response.Status = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao buscar asset {Id}.", idAsset);
                response.Status = false;
                response.Mensagem = "Erro interno ao buscar asset.";
            }
            return response;
        }

        public async Task<ResponseModel<AssetResponseDTO>> CriarAsset(AssetCriacaoDTO dto)
        {
            var response = new ResponseModel<AssetResponseDTO>();
            try
            {
                // 1. Sanitização
                var nomeSanitizado = dto.Nome?.Trim();

                // 2. Validação de Regra de Negócio
                if (string.IsNullOrWhiteSpace(nomeSanitizado))
                {
                    response.Status = false;
                    response.Mensagem = "O nome do asset é obrigatório.";
                    return response;
                }

                var existe = await _context.Assets.AnyAsync(a => a.Nome.ToLower() == nomeSanitizado.ToLower());
                if (existe)
                {
                    response.Status = false;
                    response.Mensagem = $"Já existe um asset cadastrado com o nome '{nomeSanitizado}'.";
                    return response;
                }

                // 3. Persistência
                var asset = new Asset
                {
                    Nome = nomeSanitizado,
                    Tipo = dto.Tipo,
                    Ambiente = dto.Ambiente,
                    Habilitado = dto.Habilitado
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Asset '{Nome}' criado com ID {Id}.", asset.Nome, asset.Id);

                response.Dados = ToDTO(asset);
                response.Status = true;
                response.Mensagem = "Asset criado com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar asset.");
                response.Status = false;
                response.Mensagem = "Falha técnica ao criar o asset.";
            }
            return response;
        }

        public async Task<ResponseModel<AssetResponseDTO>> EditarAsset(int idAsset, EditarAssetDTO dto)
        {
            var response = new ResponseModel<AssetResponseDTO>();
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == idAsset);
                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado.";
                    return response;
                }

                if (dto.Nome != null) 
                {
                    var nomeNovo = dto.Nome.Trim();
                    if (string.IsNullOrWhiteSpace(nomeNovo))
                    {
                        response.Status = false;
                        response.Mensagem = "O nome não pode ser vazio.";
                        return response;
                    }

                    var duplicado = await _context.Assets.AnyAsync(a => a.Nome.ToLower() == nomeNovo.ToLower() && a.Id != idAsset);
                    if (duplicado)
                    {
                        response.Status = false;
                        response.Mensagem = "Este nome já está em uso por outro asset.";
                        return response;
                    }
                    asset.Nome = nomeNovo;
                }

                if (dto.Tipo != null) asset.Tipo = dto.Tipo.Value;
                if (dto.Ambiente != null) asset.Ambiente = dto.Ambiente.Value;
                if (dto.Habilitado != null) asset.Habilitado = dto.Habilitado.Value;

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Asset {Id} atualizado.", idAsset);

                response.Dados = ToDTO(asset);
                response.Status = true;
                response.Mensagem = "Asset atualizado com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao editar asset {Id}.", idAsset);
                response.Status = false;
                response.Mensagem = "Erro ao salvar alterações do asset.";
            }
            return response;
        }

        public async Task<ResponseModel<bool>> ArquivarAsset(int idAsset)
        {
            var response = new ResponseModel<bool>();
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == idAsset && a.ArchivedAt == null);
                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado ou já arquivado.";
                    return response;
                }

                asset.ArchivedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogWarning("Asset {Id} movido para o arquivo.", idAsset);

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Asset arquivado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao arquivar asset {Id}.", idAsset);
                response.Status = false;
                response.Mensagem = "Falha ao processar arquivamento.";
            }
            return response;
        }

        public async Task<ResponseModel<bool>> RestaurarAsset(int idAsset)
        {
            var response = new ResponseModel<bool>();
            try
            {
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Id == idAsset && a.ArchivedAt != null);

                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado no arquivo.";
                    return response;
                }

                asset.ArchivedAt = null;
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Asset {Id} restaurado com sucesso.", idAsset);

                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Asset restaurado.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao restaurar asset {Id}.", idAsset);
                response.Status = false;
                response.Mensagem = "Erro técnico ao restaurar asset.";
            }
            return response;
        }

        public async Task<ResponseModel<AssetResponseDTO>> AdicionarVulnAoAsset(int idAsset, int idVuln)
        {
            var response = new ResponseModel<AssetResponseDTO>();
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == idAsset);
                var vuln = await _context.Vulns.FirstOrDefaultAsync(v => v.Id == idVuln);

                if (asset == null || vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset ou Vulnerabilidade não encontrados.";
                    return response;
                }

                var existe = await _context.AssetVulns
                    .AnyAsync(av => av.AssetId == idAsset && av.VulnId == idVuln);

                if (!existe)
                {
                    _context.AssetVulns.Add(new AssetVuln
                    {
                        AssetId = idAsset,
                        VulnId = idVuln,
                        CreatedAt = DateTime.UtcNow
                    });
                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Vulnerabilidade {VulnId} vinculada ao Asset {AssetId}.", idVuln, idAsset);
                }

                response.Dados = ToDTO(asset);
                response.Status = true;
                response.Mensagem = existe ? "Vínculo já existe." : "Vulnerabilidade vinculada com sucesso.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao vincular vuln {VulnId} ao asset {AssetId}.", idVuln, idAsset);
                response.Status = false;
                response.Mensagem = "Erro ao processar vínculo.";
            }
            return response;
        }

        public async Task<ResponseModel<bool>> RemoverVulnDoAssetAsync(int assetId, int vulnId)
        {
            var response = new ResponseModel<bool>();
            try
            {
                var relacao = await _context.AssetVulns
                    .FirstOrDefaultAsync(x => x.AssetId == assetId && x.VulnId == vulnId);

                if (relacao == null)
                {
                    response.Status = false;
                    response.Mensagem = "Vínculo não encontrado.";
                    response.Dados = false;
                    return response;
                }

                _context.AssetVulns.Remove(relacao);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Vínculo removido entre Asset {AssetId} e Vuln {VulnId}.", assetId, vulnId);

                response.Status = true;
                response.Mensagem = "Vínculo removido.";
                response.Dados = true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao remover vínculo entre asset {AssetId} e vuln {VulnId}.", assetId, vulnId);
                response.Status = false;
                response.Mensagem = "Erro ao desfazer vínculo.";
            }
            return response;
        }
    }
}
