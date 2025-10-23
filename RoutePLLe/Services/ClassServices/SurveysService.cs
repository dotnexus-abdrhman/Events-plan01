using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using EvenDAL.Repositories.InterFace;
using EventPl.Dto.Mina;
using EventPl.Services.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using RouteDAl.Data.Contexts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace EventPl.Services.ClassServices
{
    /// <summary>
    /// خدمة إدارة الاستبيانات (Surveys)
    /// </summary>
    public class SurveysService : ISurveysService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<Survey, Guid> _surveyRepo;
        private readonly IRepository<SurveyQuestion, Guid> _questionRepo;
        private readonly IRepository<SurveyOption, Guid> _optionRepo;
        private readonly IRepository<SurveyAnswer, Guid> _answerRepo;
        private readonly IRepository<SurveyAnswerOption, Guid> _answerOptionRepo;
        private readonly IMapper _mapper;
        private readonly IMemoryCache _cache;

        public SurveysService(
            AppDbContext db,
            IRepository<Survey, Guid> surveyRepo,
            IRepository<SurveyQuestion, Guid> questionRepo,
            IRepository<SurveyOption, Guid> optionRepo,
            IRepository<SurveyAnswer, Guid> answerRepo,
            IRepository<SurveyAnswerOption, Guid> answerOptionRepo,
            IMapper mapper,
            IMemoryCache cache)
        {
            _db = db;
            _surveyRepo = surveyRepo;
            _questionRepo = questionRepo;
            _optionRepo = optionRepo;
            _answerRepo = answerRepo;
            _answerOptionRepo = answerOptionRepo;
            _mapper = mapper;
            _cache = cache;
        }

        // ============================================
        // Survey Operations
        // ============================================

        public async Task<List<SurveyDto>> GetEventSurveysAsync(Guid eventId)
        {
            var version = await _db.Events.AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => (DateTime?)(e.UpdatedAt ?? e.CreatedAt))
                .FirstOrDefaultAsync();
            var ticks = version.HasValue ? version.Value.Ticks : 0L;
            var cacheKey = $"evt:{eventId}:v:{ticks}:surveys";
            if (_cache.TryGetValue(cacheKey, out List<SurveyDto> cached))
                return cached;

            var surveys = await _db.Surveys
                .AsNoTracking()
                .AsSplitQuery()
                .Where(s => s.EventId == eventId)
                .Include(s => s.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .OrderBy(s => s.Order)
                .ToListAsync();

            var dtos = _mapper.Map<List<SurveyDto>>(surveys);
            _cache.Set(cacheKey, dtos, TimeSpan.FromSeconds(45));
            return dtos;
        }
        public async Task<List<SurveyDto>> GetEventSurveysAsync(Guid eventId, long? eventVersionTicks)
        {
            var ticks = eventVersionTicks ?? (await _db.Events.AsNoTracking()
                .Where(e => e.EventId == eventId)
                .Select(e => (DateTime?)(e.UpdatedAt ?? e.CreatedAt))
                .FirstOrDefaultAsync())?.Ticks ?? 0L;

            var cacheKey = $"evt:{eventId}:v:{ticks}:surveys";
            if (_cache.TryGetValue(cacheKey, out List<SurveyDto> cached))
                return cached;

            var surveys = await _db.Surveys
                .AsNoTracking()
                .AsSplitQuery()
                .Where(s => s.EventId == eventId)
                .Include(s => s.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .OrderBy(s => s.Order)
                .ToListAsync();

            var dtos = _mapper.Map<List<SurveyDto>>(surveys);
            _cache.Set(cacheKey, dtos, TimeSpan.FromSeconds(45));
            return dtos;
        }


        public async Task<SurveyDto?> GetSurveyByIdAsync(Guid surveyId)
        {
            var survey = await _db.Surveys
                .AsNoTracking()
                .AsSplitQuery()
                .Where(s => s.SurveyId == surveyId)
                .Include(s => s.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync();

            return survey != null ? _mapper.Map<SurveyDto>(survey) : null;
        }

        public async Task<SurveyDto> CreateSurveyAsync(SurveyDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان الاستبيان مطلوب");

            if (dto.EventId == Guid.Empty)
                throw new ArgumentException("معرّف الحدث مطلوب");

            // تحديد الترتيب التلقائي
            if (dto.Order == 0)
            {
                var existingSurveys = await _surveyRepo
                    .FindAsync(s => s.EventId == dto.EventId && s.SectionId == dto.SectionId);
                dto.Order = existingSurveys.Any()
                    ? existingSurveys.Max(s => s.Order) + 1
                    : 1;
            }

            var survey = _mapper.Map<Survey>(dto);
            survey.SurveyId = Guid.NewGuid();
            survey.CreatedAt = DateTime.UtcNow;
            survey.IsActive = true;

            await _surveyRepo.AddAsync(survey);

            return _mapper.Map<SurveyDto>(survey);
        }

        public async Task<bool> UpdateSurveyAsync(SurveyDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Title))
                throw new ArgumentException("عنوان الاستبيان مطلوب");

            var survey = await _surveyRepo.GetByIdAsync(dto.SurveyId);
            if (survey == null)
                throw new KeyNotFoundException("الاستبيان غير موجود");

            survey.Title = dto.Title.Trim();
            survey.Order = dto.Order;
            survey.IsActive = dto.IsActive;

            return await _surveyRepo.UpdateAsync(survey);
        }

        public async Task<bool> DeleteSurveyAsync(Guid surveyId)
        {
            var survey = await _surveyRepo.GetByIdAsync(surveyId);
            if (survey == null)
                throw new KeyNotFoundException("الاستبيان غير موجود");

            return await _surveyRepo.DeleteByIdAsync(surveyId);
        }

        public async Task<bool> ToggleSurveyActiveAsync(Guid surveyId, bool isActive)
        {
            var survey = await _surveyRepo.GetByIdAsync(surveyId);
            if (survey == null)
                throw new KeyNotFoundException("الاستبيان غير موجود");

            survey.IsActive = isActive;
            return await _surveyRepo.UpdateAsync(survey);
        }

        // ============================================
        // Question Operations
        // ============================================

        public async Task<SurveyQuestionDto> AddQuestionAsync(SurveyQuestionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ArgumentException("نص السؤال مطلوب");

            if (dto.SurveyId == Guid.Empty)
                throw new ArgumentException("معرّف الاستبيان مطلوب");

            // التحقق من نوع السؤال
            if (!Enum.TryParse<SurveyQuestionType>(dto.Type, true, out var questionType))
                throw new ArgumentException("نوع السؤال غير صحيح. استخدم: Single أو Multiple");

            // تحديد الترتيب التلقائي
            if (dto.Order == 0)
            {
                var existingQuestions = await _questionRepo
                    .FindAsync(q => q.SurveyId == dto.SurveyId);
                dto.Order = existingQuestions.Any()
                    ? existingQuestions.Max(q => q.Order) + 1
                    : 1;
            }

            var question = _mapper.Map<SurveyQuestion>(dto);
            question.SurveyQuestionId = Guid.NewGuid();
            question.CreatedAt = DateTime.UtcNow;

            await _questionRepo.AddAsync(question);

            return _mapper.Map<SurveyQuestionDto>(question);
        }

        public async Task<bool> UpdateQuestionAsync(SurveyQuestionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ArgumentException("نص السؤال مطلوب");

            var question = await _questionRepo.GetByIdAsync(dto.SurveyQuestionId);
            if (question == null)
                throw new KeyNotFoundException("السؤال غير موجود");

            // التحقق من نوع السؤال
            if (!Enum.TryParse<SurveyQuestionType>(dto.Type, true, out var questionType))
                throw new ArgumentException("نوع السؤال غير صحيح");

            question.Text = dto.Text.Trim();
            question.Type = questionType;
            question.Order = dto.Order;
            question.IsRequired = dto.IsRequired;

            return await _questionRepo.UpdateAsync(question);
        }

        public async Task<bool> DeleteQuestionAsync(Guid questionId)
        {
            var question = await _questionRepo.GetByIdAsync(questionId);
            if (question == null)
                throw new KeyNotFoundException("السؤال غير موجود");

            return await _questionRepo.DeleteByIdAsync(questionId);
        }

        // ============================================
        // Option Operations
        // ============================================

        public async Task<SurveyOptionDto> AddOptionAsync(SurveyOptionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ArgumentException("نص الخيار مطلوب");

            if (dto.QuestionId == Guid.Empty)
                throw new ArgumentException("معرّف السؤال مطلوب");

            // تحديد الترتيب التلقائي
            if (dto.Order == 0)
            {
                var existingOptions = await _optionRepo
                    .FindAsync(o => o.QuestionId == dto.QuestionId);
                dto.Order = existingOptions.Any()
                    ? existingOptions.Max(o => o.Order) + 1
                    : 1;
            }

            var option = _mapper.Map<SurveyOption>(dto);
            option.SurveyOptionId = Guid.NewGuid();
            option.CreatedAt = DateTime.UtcNow;

            await _optionRepo.AddAsync(option);

            return _mapper.Map<SurveyOptionDto>(option);
        }

        public async Task<bool> UpdateOptionAsync(SurveyOptionDto dto)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(dto.Text))
                throw new ArgumentException("نص الخيار مطلوب");

            var option = await _optionRepo.GetByIdAsync(dto.SurveyOptionId);
            if (option == null)
                throw new KeyNotFoundException("الخيار غير موجود");

            option.Text = dto.Text.Trim();
            option.Order = dto.Order;

            return await _optionRepo.UpdateAsync(option);
        }

        public async Task<bool> DeleteOptionAsync(Guid optionId)
        {
            var option = await _optionRepo.GetByIdAsync(optionId);
            if (option == null)
                throw new KeyNotFoundException("الخيار غير موجود");

            return await _optionRepo.DeleteByIdAsync(optionId);
        }

        // ============================================
        // Answer Operations
        // ============================================

        public async Task<bool> SaveUserAnswersAsync(SaveSurveyAnswersRequest request)
        {
            // Validation
            if (request.EventId == Guid.Empty)
                throw new ArgumentException("معرّف الحدث مطلوب");

            if (request.UserId == Guid.Empty)
                throw new ArgumentException("معرّف المستخدم مطلوب");

            if (request.Answers == null || !request.Answers.Any())
                throw new ArgumentException("لا توجد إجابات للحفظ");

            // حذف الإجابات القديمة للمستخدم (إن وجدت)
            var existingAnswers = await _answerRepo
                .FindAsync(a => a.EventId == request.EventId && a.UserId == request.UserId);

            foreach (var existing in existingAnswers)
            {
                await _answerRepo.DeleteByIdAsync(existing.SurveyAnswerId);
            }

            // حفظ الإجابات الجديدة
            foreach (var answerDto in request.Answers)
            {
                if (!answerDto.SelectedOptionIds.Any())
                    continue; // تخطي الأسئلة بدون إجابة

                // إنشاء SurveyAnswer
                var answer = new SurveyAnswer
                {
                    SurveyAnswerId = Guid.NewGuid(),
                    EventId = request.EventId,
                    QuestionId = answerDto.QuestionId,
                    UserId = request.UserId,
                    CreatedAt = DateTime.UtcNow
                };

                await _answerRepo.AddAsync(answer);

                // إنشاء SurveyAnswerOptions
                foreach (var optionId in answerDto.SelectedOptionIds)
                {
                    var answerOption = new SurveyAnswerOption
                    {
                        SurveyAnswerId = answer.SurveyAnswerId,
                        OptionId = optionId,
                        CreatedAt = DateTime.UtcNow
                    };

                    await _answerOptionRepo.AddAsync(answerOption);
                }
            }

            return true;
        }

        public async Task<List<SurveyAnswerDto>> GetUserAnswersAsync(Guid eventId, Guid userId)
        {
            var answers = await _db.SurveyAnswers
                .AsNoTracking()
                .AsSplitQuery()
                .Where(a => a.EventId == eventId && a.UserId == userId)
                .Include(a => a.SelectedOptions)
                .ToListAsync();

            return _mapper.Map<List<SurveyAnswerDto>>(answers);
        }

        public async Task<bool> HasUserAnsweredAsync(Guid eventId, Guid userId)
        {
            return await _db.SurveyAnswers
                .AnyAsync(a => a.EventId == eventId && a.UserId == userId);
        }
    }
}

