using AutoMapper;
using EvenDAL.Models.Classes;
using EvenDAL.Models.Shared.Enums;
using EventPl.Dto.Mina;
using System.Text.Json;

namespace EventPl.Mapping
{
    /// <summary>
    /// AutoMapper Profile لنظام مينا للأحداث
    /// </summary>
    public class MinaEventsProfile : Profile
    {
        public MinaEventsProfile()
        {
            // ============================================
            // Section Mappings
            // ============================================
            CreateMap<Section, SectionDto>()
                .ForMember(d => d.Decisions, opt => opt.MapFrom(s => s.Decisions));
            
            CreateMap<SectionDto, Section>()
                .ForMember(e => e.Event, opt => opt.Ignore())
                .ForMember(e => e.Decisions, opt => opt.Ignore());

            // ============================================
            // Decision Mappings
            // ============================================
            CreateMap<Decision, DecisionDto>()
                .ForMember(d => d.Items, opt => opt.MapFrom(s => s.Items));
            
            CreateMap<DecisionDto, Decision>()
                .ForMember(e => e.Section, opt => opt.Ignore())
                .ForMember(e => e.Items, opt => opt.Ignore());

            // ============================================
            // DecisionItem Mappings
            // ============================================
            CreateMap<DecisionItem, DecisionItemDto>();
            CreateMap<DecisionItemDto, DecisionItem>()
                .ForMember(e => e.Decision, opt => opt.Ignore());

            // ============================================
            // Survey Mappings
            // ============================================
            CreateMap<Survey, SurveyDto>()
                .ForMember(d => d.Questions, opt => opt.MapFrom(s => s.Questions));
            
            CreateMap<SurveyDto, Survey>()
                .ForMember(e => e.Event, opt => opt.Ignore())
                .ForMember(e => e.Questions, opt => opt.Ignore());

            // ============================================
            // SurveyQuestion Mappings
            // ============================================
            CreateMap<SurveyQuestion, SurveyQuestionDto>()
                .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()))
                .ForMember(d => d.Options, opt => opt.MapFrom(s => s.Options));
            
            CreateMap<SurveyQuestionDto, SurveyQuestion>()
                .ForMember(e => e.Type, opt => opt.MapFrom(d => ParseSurveyQuestionType(d.Type)))
                .ForMember(e => e.Survey, opt => opt.Ignore())
                .ForMember(e => e.Options, opt => opt.Ignore())
                .ForMember(e => e.Answers, opt => opt.Ignore());

            // ============================================
            // SurveyOption Mappings
            // ============================================
            CreateMap<SurveyOption, SurveyOptionDto>();
            CreateMap<SurveyOptionDto, SurveyOption>()
                .ForMember(e => e.Question, opt => opt.Ignore())
                .ForMember(e => e.AnswerOptions, opt => opt.Ignore());

            // ============================================
            // SurveyAnswer Mappings
            // ============================================
            CreateMap<SurveyAnswer, SurveyAnswerDto>()
                .ForMember(d => d.SelectedOptionIds, opt => opt.MapFrom(s => 
                    s.SelectedOptions.Select(so => so.OptionId).ToList()));
            
            CreateMap<SurveyAnswerDto, SurveyAnswer>()
                .ForMember(e => e.Event, opt => opt.Ignore())
                .ForMember(e => e.Question, opt => opt.Ignore())
                .ForMember(e => e.User, opt => opt.Ignore())
                .ForMember(e => e.SelectedOptions, opt => opt.Ignore());

            // ============================================
            // Discussion Mappings
            // ============================================
            CreateMap<Discussion, DiscussionDto>()
                .ForMember(d => d.Replies, opt => opt.MapFrom(s => s.Replies));
            
            CreateMap<DiscussionDto, Discussion>()
                .ForMember(e => e.Event, opt => opt.Ignore())
                .ForMember(e => e.Replies, opt => opt.Ignore());

            // ============================================
            // DiscussionReply Mappings
            // ============================================
            CreateMap<DiscussionReply, DiscussionReplyDto>()
                .ForMember(d => d.UserName, opt => opt.MapFrom(s => s.User.FullName));
            
            CreateMap<DiscussionReplyDto, DiscussionReply>()
                .ForMember(e => e.Discussion, opt => opt.Ignore())
                .ForMember(e => e.User, opt => opt.Ignore());

            // ============================================
            // TableBlock Mappings
            // ============================================
            CreateMap<TableBlock, TableBlockDto>()
                .ForMember(d => d.TableData, opt => opt.MapFrom(s => 
                    DeserializeTableData(s.RowsJson)));
            
            CreateMap<TableBlockDto, TableBlock>()
                .ForMember(e => e.RowsJson, opt => opt.MapFrom(d => 
                    SerializeTableData(d.TableData)))
                .ForMember(e => e.Event, opt => opt.Ignore());

            // ============================================
            // Attachment Mappings
            // ============================================
            CreateMap<Attachment, AttachmentDto>()
                .ForMember(d => d.Type, opt => opt.MapFrom(s => s.Type.ToString()));
            
            CreateMap<AttachmentDto, Attachment>()
                .ForMember(e => e.Type, opt => opt.MapFrom(d => ParseAttachmentType(d.Type)))
                .ForMember(e => e.Event, opt => opt.Ignore());

            // ============================================
            // UserSignature Mappings
            // ============================================
            CreateMap<UserSignature, UserSignatureDto>();
            CreateMap<UserSignatureDto, UserSignature>()
                .ForMember(e => e.Event, opt => opt.Ignore())
                .ForMember(e => e.User, opt => opt.Ignore());
        }

        // ============================================
        // Helper Methods
        // ============================================

        private static SurveyQuestionType ParseSurveyQuestionType(string type)
        {
            return Enum.TryParse<SurveyQuestionType>(type, true, out var result)
                ? result
                : SurveyQuestionType.Single;
        }

        private static AttachmentType ParseAttachmentType(string type)
        {
            return Enum.TryParse<AttachmentType>(type, true, out var result)
                ? result
                : AttachmentType.Image;
        }

        private static TableDataDto? DeserializeTableData(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNameCaseInsensitive = true 
                };
                return JsonSerializer.Deserialize<TableDataDto>(json, options);
            }
            catch
            {
                return null;
            }
        }

        private static string SerializeTableData(TableDataDto? data)
        {
            if (data == null) return "{}";
            
            try
            {
                var options = new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false
                };
                return JsonSerializer.Serialize(data, options);
            }
            catch
            {
                return "{}";
            }
        }
    }
}

