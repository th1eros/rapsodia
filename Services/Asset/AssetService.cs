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

        // ==============================
        // LISTAR
        // ==============================
        public async Task<ResponseModel<List<AssetResponseDTO>>> ListarAssets()
        {
            var response = new ResponseModel<List<AssetResponseDTO>>();

            try
            {
                var assets = await _context.Assets.ToListAsync();

                response.Dados = assets.Select(a => new AssetResponseDTO
                {
                    Id = a.Id,
                    Nome = a.Nome,
                    CreatedAt = a.CreatedAt
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
        public async Task<ResponseModel<AssetResponseDTO>> BuscarAssetPorId(int idAsset)
        {
            var response = new ResponseModel<AssetResponseDTO>();

            try
            {
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Id == idAsset);

                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado.";
                    return response;
                }

                response.Dados = new AssetResponseDTO
                {
                    Id = asset.Id,
                    Nome = asset.Nome,
                    CreatedAt = asset.CreatedAt
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
        // VINCULAR VULN AO ASSET
        // ==============================
        public async Task<ResponseModel<AssetResponseDTO>> AdicionarVulnAoAsset(int idAsset, int idVuln)
        {
            var response = new ResponseModel<AssetResponseDTO>();

            try
            {
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Id == idAsset);

                var vuln = await _context.Vulns
                    .FirstOrDefaultAsync(v => v.Id == idVuln);

                if (asset == null || vuln == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset ou Vulnerabilidade não encontrados.";
                    return response;
                }

                var existeRelacao = await _context.AssetVulns
                    .AnyAsync(av => av.AssetId == idAsset && av.VulnId == idVuln);

                if (!existeRelacao)
                {
                    _context.AssetVulns.Add(new AssetVuln
                    {
                        AssetId = idAsset,
                        VulnId = idVuln
                    });

                    await _context.SaveChangesAsync();
                }

                response.Dados = new AssetResponseDTO
                {
                    Id = asset.Id,
                    Nome = asset.Nome,
                    CreatedAt = asset.CreatedAt
                };

                response.Status = true;
                response.Mensagem = "Vulnerabilidade vinculada com sucesso.";
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
        public async Task<ResponseModel<AssetResponseDTO>> CriarAsset(AssetCriacaoDTO dto)
        {
            var response = new ResponseModel<AssetResponseDTO>();

            try
            {
                var asset = new Asset
                {
                    Nome = dto.Nome!,
                    Tipo = dto.Tipo!,
                    Ambiente = dto.Ambiente!,   // obrigatório
                    Habilitado = dto.Habilitado
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                response.Dados = new AssetResponseDTO
                {
                    Nome = dto.Nome!,
                    Tipo = dto.Tipo!,
                    Ambiente = dto.Ambiente!,  
                    Habilitado = dto.Habilitado
                };

                response.Status = true;
                response.Mensagem = "Asset criado com sucesso.";
            }
            catch (Exception ex)
            {
                return new ResponseModel<AssetResponseDTO>
                {
                    Status = false,
                    Mensagem = ex.Message +
                                (ex.InnerException != null
                                    ? " | INNER: " + ex.InnerException.Message
                                    : "")
                };
            }

            return response;
        }

        // ==============================
        // EDITAR
        // ==============================
        public async Task<ResponseModel<AssetResponseDTO>> EditarAsset(int idAsset, EditarAssetDTO dto)
        {
            var response = new ResponseModel<AssetResponseDTO>();

            try
            {
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Id == idAsset);

                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado.";
                    return response;
                }

                asset.Nome = dto.Nome ?? asset.Nome;

                await _context.SaveChangesAsync();

                response.Dados = new AssetResponseDTO
                {
                    Id = asset.Id,
                    Nome = asset.Nome,
                    CreatedAt = asset.CreatedAt
                };

                response.Status = true;
                response.Mensagem = "Asset atualizado.";
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
        public async Task<ResponseModel<bool>> ArquivarAsset(int idAsset)
        {
            var response = new ResponseModel<bool>();

            try
            {
                var asset = await _context.Assets
                    .FirstOrDefaultAsync(a => a.Id == idAsset);

                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado.";
                    return response;
                }

                asset.DeletedAt = DateTime.UtcNow;

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

        // ==============================
        // RESTAURAR
        // ==============================
        public async Task<ResponseModel<bool>> RestaurarAsset(int idAsset)
        {
            var response = new ResponseModel<bool>();

            try
            {
                var asset = await _context.Assets
                    .IgnoreQueryFilters()
                    .FirstOrDefaultAsync(a => a.Id == idAsset);

                if (asset == null)
                {
                    response.Status = false;
                    response.Mensagem = "Asset não encontrado.";
                    return response;
                }

                asset.DeletedAt = null;

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
        // ==============================
        // REMOVER VULN DO ASSET
        // ==============================
        public async Task<ResponseModel<bool>> RemoverVulnDoAssetAsync(int assetId, int vulnId)
        {
            var response = new ResponseModel<bool>();

            var relacao = await _context.AssetVulns
                .FirstOrDefaultAsync(x =>
                    x.AssetId == assetId &&
                    x.VulnId == vulnId);

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
            response.Mensagem = "Vínculo removido com sucesso.";
            response.Dados = true;

            return response;
        }
    }
}