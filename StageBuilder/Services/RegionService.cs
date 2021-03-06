using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using AutoMapper;

using StageBuilder.Database;
using StageBuilder.Models;
using StageBuilder.Dtos;

namespace StageBuilder.Services
{
  public class RegionService : IRegionService
  {
    public readonly StageBuilderDbContext _context;
    public readonly IStageService _stageService;
    private readonly IMapper _mapper;

    public RegionService(StageBuilderDbContext context, IStageService stageService, IMapper mapper)
    {
      _context = context;
      _stageService = stageService;
      _mapper = mapper;
    }

    public async Task<List<RegionEntity>> GetAllRegionsAsync()
    {
      return await _context.Regions.ToListAsync();
    }

    public async Task<List<RegionEntity>> GetAllRegionsForStageAsync(int stageId)
    {
      return await _context.Regions
        .Where(r => r.StageId == stageId)
        .ToListAsync<RegionEntity>();
    }

    public async Task<RegionEntity> GetRegionByRowAndColumnAsync(int stageId, int row, int column)
    {
      return await _context.Regions.FirstOrDefaultAsync(r => r.StageId == stageId && r.Row == row && r.Column == column);
    }

    public async Task<List<RegionEntity>> GetRegionNeighborsAsync(int stageId, int row, int column)
    {
      return await _context.Regions
        .Where(r => r.StageId == stageId &&
          ((r.Row == row + 1 || r.Row == row - 1) && (r.Column == column + 1 || r.Column == column - 1)) ||
          (r.Row == row && (r.Column == column + 1 || r.Column == column - 1)) ||
          ((r.Row == row + 1 || r.Row == row - 1) && r.Column == column))
        .ToListAsync<RegionEntity>();
    }

    public async Task<RegionEntity> AddOrUpdateRegionAsync(Region dto)
    {
      var region = await fetchRegionAsync(dto);
      if (region == null)
      {
        var stage = await _stageService.GetStageByIdAsync(dto.StageId);
        await _stageService.UpdateStageBoundaries(stage, dto.Row, dto.Column);
        region = _mapper.Map<RegionEntity>(dto);
        region.CreatedDate = DateTime.Now;
        region.LastUpdatedDate = region.CreatedDate;
        _context.Regions.Add(region);
      }
      else
      {
        region.Data = dto.Data;
        region.LastUpdatedDate = DateTime.Now;
      }

      if (await _context.SaveChangesAsync() > 0) return region;
      else throw new Exception("Failed to save to the database");
    }

    private async Task<RegionEntity> fetchRegionAsync(RegionEntity region)
    {
      return await _context.Regions.FirstOrDefaultAsync(r => r.StageId == region.StageId && r.Row == region.Row && r.Column == region.Column);
    }
  }
}
