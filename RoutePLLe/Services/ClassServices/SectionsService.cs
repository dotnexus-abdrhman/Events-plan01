using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using RouteDAl.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    /// <summary>
    /// خدمة إدارة البنود (Sections) والقرارات (Decisions)
    /// </summary>
    public class SectionsService : ISectionsService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<Section, Guid> _sectionRepo;
        private readonly IRepository<Decision, Guid> _decisionRepo;
        private readonly IRepository<DecisionItem, Guid> _itemRepo;
        private readonly IMapper _mapper;

        public SectionsService(
            AppDbContext db,
            IRepository<Section, Guid> sectionRepo,
            IRepository<Decision, Guid> decisionRepo,
            IRepository<DecisionItem, Guid> itemRepo,
            IMapper mapper)
        {
            _db = db;
            _sectionRepo = sectionRepo;
            _decisionRepo = decisionRepo;
            _itemRepo = itemRepo;
            _mapper = mapper;
        }

        // ============================================
        // Section Operations
        // ============================================

        public async Task<List<SectionDto>> GetEventSectionsAsync(Guid eventId)
        {
            var sections = await _db.Sections
                .AsNoTracking()
                .Where(s => s.EventId == eventId)
                .Include(s => s.Decisions.OrderBy(d => d.Order))
                    .ThenInclude(d => d.Items.OrderBy(i => i.Order))
                .OrderBy(s => s.Order)
                .ToListAsync();

            return _mapper.Map<List<SectionDto>>(sections);
        }

        public async Task<SectionDto?> GetSectionByIdAsync(Guid sectionId)
        {
            var section = await _db.Sections
                .AsNoTracking()
                .Where(s => s.SectionId == sectionId)
                .Include(s => s.Decisions.OrderBy(d => d.Order))
                    .ThenInclude(d => d.Items.OrderBy(i => i.Order))
                .FirstOrDefaultAsync();

            return section != null ? _mapper.Map<SectionDto>(section) : null;
        }

        public async Task<SectionDto> CreateSectionAsync(SectionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان البند مطلوب");

            if (dto.EventId == Guid.Empty)
                throw new ArgumentException("معرّف الحدث مطلوب");

            // تحديد الترتيب التلقائي إذا لم يُحدد
            if (dto.Order == 0)
            {
                var existingSections = await _sectionRepo
                    .FindAsync(s => s.EventId == dto.EventId);
                dto.Order = existingSections.Any() 
                    ? existingSections.Max(s => s.Order) + 1 
                    : 1;
            }

            var section = _mapper.Map<Section>(dto);
            section.SectionId = Guid.NewGuid();
            section.CreatedAt = DateTime.UtcNow;

            await _sectionRepo.AddAsync(section);

            return _mapper.Map<SectionDto>(section);
        }

        public async Task<bool> UpdateSectionAsync(SectionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان البند مطلوب");

            var section = await _sectionRepo.GetByIdAsync(dto.SectionId);
            if (section == null)
                throw new KeyNotFoundException("البند غير موجود");

            // تحديث الحقول
            section.Title = dto.Title.Trim();
            section.Body = dto.Body ?? string.Empty;
            section.Order = dto.Order;

            return await _sectionRepo.UpdateAsync(section);
        }

        public async Task<bool> DeleteSectionAsync(Guid sectionId)
        {
            var section = await _sectionRepo.GetByIdAsync(sectionId);
            if (section == null)
                throw new KeyNotFoundException("البند غير موجود");

            return await _sectionRepo.DeleteByIdAsync(sectionId);
        }

        public async Task<bool> ReorderSectionsAsync(Guid eventId, List<Guid> sectionIds)
        {
            if (sectionIds == null || !sectionIds.Any())
                throw new ArgumentException("قائمة البنود فارغة");

            var sections = await _sectionRepo
                .FindAsync(s => s.EventId == eventId && sectionIds.Contains(s.SectionId));

            var sectionsList = sections.ToList();
            
            for (int i = 0; i < sectionIds.Count; i++)
            {
                var section = sectionsList.FirstOrDefault(s => s.SectionId == sectionIds[i]);
                if (section != null)
                {
                    section.Order = i + 1;
                    await _sectionRepo.UpdateAsync(section);
                }
            }

            return true;
        }

        // ============================================
        // Decision Operations
        // ============================================

        public async Task<DecisionDto> AddDecisionAsync(DecisionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان القرار مطلوب");

            if (dto.SectionId == Guid.Empty)
                throw new ArgumentException("معرّف البند مطلوب");

            // تحديد الترتيب التلقائي
            if (dto.Order == 0)
            {
                var existingDecisions = await _decisionRepo
                    .FindAsync(d => d.SectionId == dto.SectionId);
                dto.Order = existingDecisions.Any() 
                    ? existingDecisions.Max(d => d.Order) + 1 
                    : 1;
            }

            var decision = _mapper.Map<Decision>(dto);
            decision.DecisionId = Guid.NewGuid();
            decision.CreatedAt = DateTime.UtcNow;

            await _decisionRepo.AddAsync(decision);

            return _mapper.Map<DecisionDto>(decision);
        }

        public async Task<bool> UpdateDecisionAsync(DecisionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان القرار مطلوب");

            var decision = await _decisionRepo.GetByIdAsync(dto.DecisionId);
            if (decision == null)
                throw new KeyNotFoundException("القرار غير موجود");

            decision.Title = dto.Title.Trim();
            decision.Order = dto.Order;

            return await _decisionRepo.UpdateAsync(decision);
        }

        public async Task<bool> DeleteDecisionAsync(Guid decisionId)
        {
            var decision = await _decisionRepo.GetByIdAsync(decisionId);
            if (decision == null)
                throw new KeyNotFoundException("القرار غير موجود");

            return await _decisionRepo.DeleteByIdAsync(decisionId);
        }

        // ============================================
        // DecisionItem Operations
        // ============================================

        public async Task<DecisionItemDto> AddDecisionItemAsync(DecisionItemDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ArgumentException("نص العنصر مطلوب");

            if (dto.DecisionId == Guid.Empty)
                throw new ArgumentException("معرّف القرار مطلوب");

            // تحديد الترتيب التلقائي
            if (dto.Order == 0)
            {
                var existingItems = await _itemRepo
                    .FindAsync(i => i.DecisionId == dto.DecisionId);
                dto.Order = existingItems.Any() 
                    ? existingItems.Max(i => i.Order) + 1 
                    : 1;
            }

            var item = _mapper.Map<DecisionItem>(dto);
            item.DecisionItemId = Guid.NewGuid();
            item.CreatedAt = DateTime.UtcNow;

            await _itemRepo.AddAsync(item);

            return _mapper.Map<DecisionItemDto>(item);
        }

        public async Task<bool> UpdateDecisionItemAsync(DecisionItemDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ArgumentException("نص العنصر مطلوب");

            var item = await _itemRepo.GetByIdAsync(dto.DecisionItemId);
            if (item == null)
                throw new KeyNotFoundException("العنصر غير موجود");

            item.Text = dto.Text.Trim();
            item.Order = dto.Order;

            return await _itemRepo.UpdateAsync(item);
        }

        public async Task<bool> DeleteDecisionItemAsync(Guid itemId)
        {
            var item = await _itemRepo.GetByIdAsync(itemId);
            if (item == null)
                throw new KeyNotFoundException("العنصر غير موجود");

            return await _itemRepo.DeleteByIdAsync(itemId);
        }
    }
}

