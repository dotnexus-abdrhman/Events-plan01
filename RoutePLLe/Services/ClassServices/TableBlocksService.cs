using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using RouteDAl.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    /// <summary>
    /// خدمة إدارة الجداول المرنة (TableBlocks)
    /// مع معالجة JSON Case-Insensitive
    /// </summary>
    public class TableBlocksService : ITableBlocksService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<TableBlock, Guid> _tableRepo;
        private readonly IMapper _mapper;
        private readonly ILogger<TableBlocksService> _logger;
        private readonly IMemoryCache _cache;

        public TableBlocksService(
            AppDbContext db,
            IRepository<TableBlock, Guid> tableRepo,
            IMapper mapper,
            ILogger<TableBlocksService> logger,
            IMemoryCache cache)
        {
            _db = db;
            _tableRepo = tableRepo;
            _mapper = mapper;
            _logger = logger;
            _cache = cache;
        }

        public async Task<List<TableBlockDto>> GetEventTablesAsync(Guid eventId)
        {
            var version = await _db.Events.AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => (DateTime?)(e.UpdatedAt ?? e.CreatedAt))
                .FirstOrDefaultAsync();
            var ticks = version.HasValue ? version.Value.Ticks : 0L;
            var cacheKey = $"evt:{eventId}:v:{ticks}:tables";
            if (_cache.TryGetValue(cacheKey, out List<TableBlockDto> cached))
                return cached;

            var tables = await _db.TableBlocks
                .AsNoTracking()
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.Order)
                .ToListAsync();

            var dtos = _mapper.Map<List<TableBlockDto>>(tables);

            // Fallback hydration: if TableData is null (legacy or unexpected JSON), try to parse RowsJson manually
            for (int i = 0; i < dtos.Count && i < tables.Count; i++)
            {
                if (dtos[i].TableData == null && !string.IsNullOrWhiteSpace(tables[i].RowsJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(tables[i].RowsJson);
                        if (doc.RootElement.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var rows = new List<EventPl.Dto.Mina.TableRowDto>();
                            foreach (var rowEl in rowsEl.EnumerateArray())
                            {
                                var row = new EventPl.Dto.Mina.TableRowDto();
                                if (rowEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var cellEl in rowEl.EnumerateArray())
                                    {
                                        // support either string cells or objects with { value }
                                        if (cellEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                        {
                                            row.Cells.Add(cellEl.GetString() ?? string.Empty);
                                        }
                                        else if (cellEl.ValueKind == System.Text.Json.JsonValueKind.Object && cellEl.TryGetProperty("value", out var v))
                                        {
                                            row.Cells.Add(v.GetString() ?? string.Empty);
                                        }
                                        else

                                        {
                                            row.Cells.Add(string.Empty);
                                        }
                                    }
                                }
                                else if (rowEl.ValueKind == System.Text.Json.JsonValueKind.Object && rowEl.TryGetProperty("cells", out var cellsEl) && cellsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var cellEl in cellsEl.EnumerateArray())
                                    {
                                        row.Cells.Add(cellEl.ValueKind == System.Text.Json.JsonValueKind.String ? (cellEl.GetString() ?? string.Empty) : string.Empty);
                                    }
                                }
                                dtos[i].TableData ??= new EventPl.Dto.Mina.TableDataDto();
                                dtos[i].TableData.Rows.Add(row);
                            }
                        }
                    }
                    catch { /* ignore malformed */ }
                }
            }

            _cache.Set(cacheKey, dtos, TimeSpan.FromSeconds(45));
            return dtos;
        }

        public async Task<TableBlockDto?> GetTableByIdAsync(Guid tableBlockId)
        {
            var table = await _tableRepo.GetByIdAsync(tableBlockId);
            return table != null ? _mapper.Map<TableBlockDto>(table) : null;
        }

        public async Task<TableBlockDto> CreateTableAsync(TableBlockDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان الجدول مطلوب");

            if (dto.EventId == Guid.Empty)
                throw new ArgumentException("معرّف الحدث مطلوب");

            // تحديد الترتيب التلقائي
            if (dto.Order == 0)
            {
                var existingTables = await _tableRepo
                    .FindAsync(t => t.EventId == dto.EventId && t.SectionId == dto.SectionId);
                dto.Order = existingTables.Any()
                    ? existingTables.Max(t => t.Order) + 1
                    : 1;
            }

            var table = _mapper.Map<TableBlock>(dto);
            table.TableBlockId = Guid.NewGuid();
            table.CreatedAt = DateTime.UtcNow;

            // معالجة JSON بشكل آمن
            if (dto.TableData != null)
            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };
                    table.RowsJson = JsonSerializer.Serialize(dto.TableData, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحويل بيانات الجدول إلى JSON");
                    table.RowsJson = "{}";
                }
            }
            else
            {
                table.RowsJson = "{}";
            }

            await _tableRepo.AddAsync(table);

            return _mapper.Map<TableBlockDto>(table);
        }

        public async Task<bool> UpdateTableAsync(TableBlockDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان الجدول مطلوب");

            var table = await _tableRepo.GetByIdAsync(dto.TableBlockId);
            if (table == null)
                throw new KeyNotFoundException("الجدول غير موجود");

            table.Title = dto.Title.Trim();
            table.Description = dto.Description ?? string.Empty;
            table.HasHeader = dto.HasHeader;
            table.Order = dto.Order;

            // معالجة JSON بشكل آمن (Case-Insensitive)
            if (dto.TableData != null)

            {
                try
                {
                    var options = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = false
                    };
                    table.RowsJson = JsonSerializer.Serialize(dto.TableData, options);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "خطأ في تحويل بيانات الجدول إلى JSON");
                    // لا نرمي Exception - نحتفظ بالقيمة القديمة
                }
            }

            return await _tableRepo.UpdateAsync(table);
        }

        public async Task<bool> DeleteTableAsync(Guid tableBlockId)
        {
            var table = await _tableRepo.GetByIdAsync(tableBlockId);
            if (table == null)
                throw new KeyNotFoundException("الجدول غير موجود");

            return await _tableRepo.DeleteByIdAsync(tableBlockId);
        }
        public async Task<List<TableBlockDto>> GetEventTablesAsync(Guid eventId, long? eventVersionTicks)
        {
            var ticks = eventVersionTicks ?? (await _db.Events.AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => (DateTime?)(e.UpdatedAt ?? e.CreatedAt))
                .FirstOrDefaultAsync())?.Ticks ?? 0L;

            var cacheKey = $"evt:{eventId}:v:{ticks}:tables";
            if (_cache.TryGetValue(cacheKey, out List<TableBlockDto> cached))
                return cached;

            var tables = await _db.TableBlocks
                .AsNoTracking()
                .Where(t => t.EventId == eventId)
                .OrderBy(t => t.Order)
                .ToListAsync();

            var dtos = _mapper.Map<List<TableBlockDto>>(tables);

            // Fallback hydration: if TableData is null (legacy or unexpected JSON), try to parse RowsJson manually
            for (int i = 0; i < dtos.Count && i < tables.Count; i++)
            {
                if (dtos[i].TableData == null && !string.IsNullOrWhiteSpace(tables[i].RowsJson))
                {
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(tables[i].RowsJson);
                        if (doc.RootElement.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            var rows = new List<EventPl.Dto.Mina.TableRowDto>();
                            foreach (var rowEl in rowsEl.EnumerateArray())
                            {
                                var row = new EventPl.Dto.Mina.TableRowDto();
                                if (rowEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var cellEl in rowEl.EnumerateArray())
                                    {
                                        // support either string cells or objects with { value }
                                        if (cellEl.ValueKind == System.Text.Json.JsonValueKind.String)
                                        {
                                            row.Cells.Add(cellEl.GetString() ?? string.Empty);
                                        }
                                        else if (cellEl.ValueKind == System.Text.Json.JsonValueKind.Object && cellEl.TryGetProperty("value", out var v))
                                        {
                                            row.Cells.Add(v.GetString() ?? string.Empty);
                                        }
                                        else
                                        {
                                            row.Cells.Add(string.Empty);
                                        }
                                    }
                                }
                                else if (rowEl.ValueKind == System.Text.Json.JsonValueKind.Object && rowEl.TryGetProperty("cells", out var cellsEl) && cellsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                                {
                                    foreach (var cellEl in cellsEl.EnumerateArray())
                                    {
                                        row.Cells.Add(cellEl.ValueKind == System.Text.Json.JsonValueKind.String ? (cellEl.GetString() ?? string.Empty) : string.Empty);
                                    }
                                }
                                dtos[i].TableData ??= new EventPl.Dto.Mina.TableDataDto();
                                dtos[i].TableData.Rows.Add(row);
                            }
                        }
                    }
                    catch { /* ignore malformed */ }
                }
            }

            _cache.Set(cacheKey, dtos, TimeSpan.FromSeconds(45));
            return dtos;
        }

    }
}

