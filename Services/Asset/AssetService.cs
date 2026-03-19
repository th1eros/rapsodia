using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API_SVsharp.Data;
using API_SVsharp.DTO.Asset;
using API_SVsharp.DTO.Response;
using API_SVsharp.Models.Entity;
using API_SVsharp.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace API_SVsharp.Services.Assets
{
    public class AssetService : IAssetService
    {
        private readonly AppDbContext _context;

        public AssetService(AppDbContext context)
        {
            _context = context;
        }

        private static AssetResponseDTO ToDTO(Asset a) => new()
        {
            Id = a.Id,
            Nome = a.Nome,
            Tipo = a.Tipo,
            Ambiente = a.Ambiente,
            Habilitado = a.Habilitado,
            CreatedAt = a.CreatedAt
        };

        public async Task<ResponseModel<List<AssetResponseDTO>>> ListarAssets()
        {
            var response = new ResponseModel<List<AssetResponseDTO>>();
            try
            {
                var assets = await _context.Assets.Where(a => a.ArchivedAt == null).ToListAsync();
                response.Dados = assets.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel<List<AssetResponseDTO>>> ListarAssetsArquivados()
        {
            var response = new ResponseModel<List<AssetResponseDTO>>();
            try
            {
                var assets = await _context.Assets.Where(a => a.ArchivedAt != null).ToListAsync();
                response.Dados = assets.Select(ToDTO).ToList();
                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel<AssetResponseDTO>> BuscarAssetPorId(int idAsset)
        {
            var response = new ResponseModel<AssetResponseDTO>();
            try
            {
                var asset = await _context.Assets.FirstOrDefaultAsync(a => a.Id == idAsset && a.ArchivedAt == null);
                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado ou arquivado.";
                    return response;
                }
                response.Dados = ToDTO(asset);
                response.Status = true;
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
            }
            return response;
        }

        public async Task<ResponseModel<AssetResponseDTO>> CriarAsset(AssetCriacaoDTO dto)
        {
            var response = new ResponseModel<AssetResponseDTO>();
            try
            {
                var asset = new Asset
                {
                    Nome = dto.Nome,
                    Tipo = dto.Tipo,
                    Ambiente = dto.Ambiente,
                    Habilitado = dto.Habilitado
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                response.Dados = ToDTO(asset);
                response.Status = true;
                response.Mensagem = "Asset criado com sucesso.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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

                if (dto.Nome != null) asset.Nome = dto.Nome;
                if (dto.Tipo != null) asset.Tipo = dto.Tipo.Value;
                if (dto.Ambiente != null) asset.Ambiente = dto.Ambiente.Value;
                if (dto.Habilitado != null) asset.Habilitado = dto.Habilitado.Value;

                await _context.SaveChangesAsync();
                response.Dados = ToDTO(asset);
                response.Status = true;
                response.Mensagem = "Asset editado.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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
                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Asset arquivado.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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
                response.Dados = true;
                response.Status = true;
                response.Mensagem = "Asset restaurado.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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
                }

                response.Dados = ToDTO(asset);
                response.Status = true;
                response.Mensagem = existe ? "Vínculo já existia." : "Vulnerabilidade vinculada.";
            }
            catch (Exception ex)
            {
                response.Status = false;
                response.Mensagem = ex.Message;
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
                response.Status = true;
                response.Mensagem = "Vínculo removido.";
                response.Dados = true;
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
