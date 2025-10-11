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
    /// خدمة عرض نتائج الأحداث (Surveys, Discussions, Signatures)
    /// </summary>
    public class MinaResultsService : IMinaResultsService
    {
        private readonly AppDbContext _db;
        private readonly IRepository<Event, Guid> _eventRepo;

        public MinaResultsService(
            AppDbContext db,
            IRepository<Event, Guid> eventRepo)
        {
            _db = db;
            _eventRepo = eventRepo;
        }

        public async Task<EventResultsSummaryDto> GetEventResultsAsync(Guid eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null)
                throw new KeyNotFoundException("الحدث غير موجود");

            // الحصول على جميع الاستبيانات
            var surveys = await _db.Surveys
                .AsNoTracking()
                .Where(s => s.EventId == eventId)
                .Include(s => s.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .OrderBy(s => s.Order)
                .ToListAsync();

            var surveyResults = new List<SurveyResultsDto>();

            foreach (var survey in surveys)
            {
                var surveyResult = await GetSurveyResultsAsync(survey.SurveyId);
                surveyResults.Add(surveyResult);
            }

            // إحصائيات عامة
            var totalParticipants = await _db.SurveyAnswers
                .Where(a => a.EventId == eventId)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            var totalSignatures = await _db.UserSignatures
                .Where(s => s.EventId == eventId)
                .CountAsync();

            var totalReplies = await _db.DiscussionReplies
                .Where(r => r.Discussion.EventId == eventId)
                .CountAsync();

            return new EventResultsSummaryDto
            {
                EventId = eventId,
                EventTitle = ev.Title,
                SurveyResults = surveyResults,
                TotalParticipants = totalParticipants,
                TotalSignatures = totalSignatures,
                TotalDiscussionReplies = totalReplies
            };
        }

        public async Task<SurveyResultsDto> GetSurveyResultsAsync(Guid surveyId)
        {
            var survey = await _db.Surveys
                .AsNoTracking()
                .Include(s => s.Questions.OrderBy(q => q.Order))
                    .ThenInclude(q => q.Options.OrderBy(o => o.Order))
                .FirstOrDefaultAsync(s => s.SurveyId == surveyId);

            if (survey == null)
                throw new KeyNotFoundException("الاستبيان غير موجود");

            var questionResults = new List<QuestionResultDto>();

            foreach (var question in survey.Questions)
            {
                // الحصول على جميع الإجابات لهذا السؤال
                var answers = await _db.SurveyAnswers
                    .AsNoTracking()
                    .Where(a => a.QuestionId == question.SurveyQuestionId)
                    .Include(a => a.SelectedOptions)
                    .ToListAsync();

                var totalAnswers = answers.Count;

                // حساب عدد الاختيارات لكل خيار
                var optionResults = new List<OptionResultDto>();

                foreach (var option in question.Options)
                {
                    var count = await _db.SurveyAnswerOptions
                        .Where(ao => ao.OptionId == option.SurveyOptionId)
                        .CountAsync();

                    var percentage = totalAnswers > 0 
                        ? $"{(count * 100.0 / totalAnswers):F1}%" 
                        : "0%";

                    optionResults.Add(new OptionResultDto
                    {
                        OptionId = option.SurveyOptionId,
                        OptionText = option.Text,
                        Count = count,
                        Percentage = percentage
                    });
                }

                questionResults.Add(new QuestionResultDto
                {
                    QuestionId = question.SurveyQuestionId,
                    QuestionText = question.Text,
                    QuestionType = question.Type.ToString(),
                    TotalAnswers = totalAnswers,
                    OptionResults = optionResults
                });
            }

            return new SurveyResultsDto
            {
                SurveyId = surveyId,
                SurveyTitle = survey.Title,
                QuestionResults = questionResults
            };
        }

        public async Task<EventStatisticsDto> GetEventStatisticsAsync(Guid eventId)
        {
            var ev = await _eventRepo.GetByIdAsync(eventId);
            if (ev == null)
                throw new KeyNotFoundException("الحدث غير موجود");

            // حساب الإحصائيات بشكل تسلسلي لتجنب تعارض EF (DbContext لا يدعم العمليات المتزامنة)
            var totalSections = await _db.Sections.CountAsync(s => s.EventId == eventId);
            var totalDecisions = await _db.Decisions.CountAsync(d => d.Section.EventId == eventId);
            var totalSurveys = await _db.Surveys.CountAsync(s => s.EventId == eventId);
            var totalQuestions = await _db.SurveyQuestions.CountAsync(q => q.Survey.EventId == eventId);
            var totalAnswers = await _db.SurveyAnswers.CountAsync(a => a.EventId == eventId);
            var totalDiscussions = await _db.Discussions.CountAsync(d => d.EventId == eventId);
            var totalReplies = await _db.DiscussionReplies.CountAsync(r => r.Discussion.EventId == eventId);
            var totalTables = await _db.TableBlocks.CountAsync(t => t.EventId == eventId);
            var totalAttachments = await _db.Attachments.CountAsync(a => a.EventId == eventId);
            var totalSignatures = await _db.UserSignatures.CountAsync(s => s.EventId == eventId);

            // حساب المشاركين الفريدين
            var uniqueParticipants = await _db.SurveyAnswers
                .Where(a => a.EventId == eventId)
                .Select(a => a.UserId)
                .Distinct()
                .CountAsync();

            return new EventStatisticsDto
            {
                EventId = eventId,
                EventTitle = ev.Title,
                TotalSections = totalSections,
                TotalDecisions = totalDecisions,
                TotalSurveys = totalSurveys,
                TotalSurveyQuestions = totalQuestions,
                TotalSurveyAnswers = totalAnswers,
                TotalDiscussions = totalDiscussions,
                TotalDiscussionReplies = totalReplies,
                TotalTables = totalTables,
                TotalAttachments = totalAttachments,
                TotalSignatures = totalSignatures,
                UniqueParticipants = uniqueParticipants
            };
        }
    }
}

