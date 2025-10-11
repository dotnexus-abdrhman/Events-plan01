using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RourtPPl01.Areas.Admin.ViewModels;
using System.Security.Claims;
using EvenDAL.Models.Shared.Enums;
using EventPl.Dto.Mina;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc.Rendering;


namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class EventsController : Controller
    {
        private readonly IMinaEventsService _eventsService;
        private readonly ILogger<EventsController> _logger;
        private readonly ISectionsService _sectionsService;
        private readonly ISurveysService _surveysService;
        private readonly IDiscussionsService _discussionsService;
        private readonly ITableBlocksService _tablesService;
        private readonly IAttachmentsService _attachmentsService;
        private readonly ICrudService<UserDto, Guid> _usersService;
        private readonly ICrudService<OrganizationDto, Guid> _orgsService;

        public EventsController(
            IMinaEventsService eventsService,
            ISectionsService sectionsService,
            ISurveysService surveysService,
            IDiscussionsService discussionsService,
            ITableBlocksService tablesService,
            IAttachmentsService attachmentsService,
            ICrudService<UserDto, Guid> usersService,
            ICrudService<OrganizationDto, Guid> orgsService,
            ILogger<EventsController> logger)
        {
            _eventsService = eventsService;
            _sectionsService = sectionsService;
            _surveysService = surveysService;
            _discussionsService = discussionsService;
            _tablesService = tablesService;
            _attachmentsService = attachmentsService;
            _usersService = usersService;
            _orgsService = orgsService;
            _logger = logger;
        }

        // ============================================
        // GET: Admin/Events
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            try
            {
                var orgId = GetOrganizationId();

                List<EventDto> events;
                if (User.IsInRole("Admin"))
                {
                    // Admins can see events across all organizations - fetch sequentially to avoid DbContext concurrency
                    var orgs = await _orgsService.ListAsync();
                    var all = new List<EventDto>();
                    foreach (var o in orgs)
                    {
                        var list = await _eventsService.GetOrganizationEventsAsync(o.OrganizationId);
                        if (list != null && list.Count > 0)
                            all.AddRange(list);
                    }
                    events = all.OrderByDescending(e => e.StartAt).ToList();
                }
                else
                {
                    events = await _eventsService.GetOrganizationEventsAsync(orgId);
                }

                var vm = new EventsIndexViewModel
                {
                    Events = events.Select(e => new EventListItemViewModel
                    {
                        EventId = e.EventId,
                        Title = e.Title,
                        Description = e.Description,
                        StartAt = e.StartAt,
                        EndAt = e.EndAt,
                        Status = Enum.TryParse<EventStatus>(e.StatusName, out var status) ? status : EventStatus.Draft,
                        RequireSignature = e.RequireSignature
                    }).ToList()
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل قائمة الأحداث");
                TempData["Error"] = "حدث خطأ أثناء تحميل الأحداث";
                return View(new EventsIndexViewModel());
            }
        }

        // ============================================
        // GET: Admin/Events/Details/5
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Details(Guid id)
        {
            try
            {
                var orgId = GetOrganizationId();
                var eventDto = await _eventsService.GetEventByIdAsync(id);

                if (eventDto == null)
                {
                    TempData["Error"] = "الحدث غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                // التحقق من الصلاحية: اسمح للمشرف (Admin) بعرض جميع الأحداث بغض النظر عن الجهة
                if (!User.IsInRole("Admin") && eventDto.OrganizationId != orgId)
                {
                    _logger.LogWarning("محاولة وصول غير مصرح بها للحدث {EventId}", id);
                    return Forbid();
                }

                var vm = new EventDetailsViewModel
                {
                    EventId = eventDto.EventId,
                    Title = eventDto.Title,
                    Description = eventDto.Description ?? string.Empty,
                    StartAt = eventDto.StartAt,
                    EndAt = eventDto.EndAt,
                    Status = Enum.TryParse<EventStatus>(eventDto.StatusName, out var status) ? status : EventStatus.Draft,
                    RequireSignature = eventDto.RequireSignature
                };

                // Load all components
                var sections = await _sectionsService.GetEventSectionsAsync(id);
                var surveys = await _surveysService.GetEventSurveysAsync(id);
                var discussions = await _discussionsService.GetEventDiscussionsAsync(id);
                var tables = await _tablesService.GetEventTablesAsync(id);
                var attachments = await _attachmentsService.GetEventAttachmentsAsync(id);

                vm.Sections = sections.Select(s => new SectionViewModel
                {
                    SectionId = s.SectionId,
                    Title = s.Title,
                    Body = s.Body,
                    Order = s.Order,
                    Decisions = (s.Decisions ?? new()).Select(d => new DecisionViewModel
                    {
                        DecisionId = d.DecisionId,
                        Title = d.Title,
                        Order = d.Order,
                        Items = (d.Items ?? new()).Select(i => new DecisionItemViewModel
                        {
                            DecisionItemId = i.DecisionItemId,
                            Text = i.Text,
                            Order = i.Order
                        }).ToList()
                    }).ToList()
                }).ToList();

                vm.Surveys = surveys.Select(s => new SurveyViewModel
                {
                    SurveyId = s.SurveyId,
                    SectionId = s.SectionId,
                    Title = s.Title,
                    Description = null,
                    Questions = (s.Questions ?? new()).Select(q => new QuestionViewModel
                    {
                        SurveyQuestionId = q.SurveyQuestionId,
                        Text = q.Text,
                        Type = Enum.TryParse<SurveyQuestionType>(q.Type, out var qType) ? qType : SurveyQuestionType.Single,
                        IsRequired = q.IsRequired,
                        Options = (q.Options ?? new()).Select(o => new OptionViewModel
                        {
                            SurveyOptionId = o.SurveyOptionId,
                            Text = o.Text
                        }).ToList()
                    }).ToList()
                }).ToList();

                vm.Discussions = discussions.Select(d => new DiscussionViewModel
                {
                    DiscussionId = d.DiscussionId,
                    SectionId = d.SectionId,
                    Title = d.Title,
                    Purpose = d.Purpose
                }).ToList();

                vm.Tables = tables.Select(t => new TableViewModel
                {
                    TableBlockId = t.TableBlockId,
                    SectionId = t.SectionId,
                    Title = t.Title,
                    HasHeader = t.HasHeader,
                    Rows = t.TableData?.Rows?.Select(row => row.Cells.Select(cell => new TableCellViewModel { Value = cell }).ToList()).ToList()
                }).ToList();

                vm.Attachments = attachments.Select(a => new AttachmentViewModel
                {
                    AttachmentId = a.AttachmentId,
                    SectionId = a.SectionId,
                    FileName = a.FileName,
                    Path = a.Path,
                    Type = Enum.TryParse<AttachmentType>(a.Type, out var aType) ? aType : AttachmentType.Image,
                    Order = a.Order
                }).ToList();

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل تفاصيل الحدث {EventId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل تفاصيل الحدث";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================
        // GET: Admin/Events/Create
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var orgs = await _orgsService.ListAsync();
            // اجعل ترتيب الجهات ثابتًا بحيث تظهر الجهة الافتراضية (seed) أولاً دائمًا
            var orderedOrgs = orgs
                .OrderBy(o => o.CreatedAt)
                .ThenBy(o => string.IsNullOrWhiteSpace(o.Name) ? (o.NameEn ?? "Organization") : o.Name)
                .ToList();

            var vm = new CreateEventViewModel
            {
                StartAt = DateTime.Now,
                EndAt = DateTime.Now.AddHours(2),
                RequireSignature = false,
                Organizations = orderedOrgs.Select(o => new SelectListItem
                {
                    Value = o.OrganizationId.ToString(),
                    Text = string.IsNullOrWhiteSpace(o.Name) ? (o.NameEn ?? "Organization") : o.Name
                }).ToList()
            };

            return View(vm);
        }

        // ============================================
        // POST: Admin/Events/Create
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateEventViewModel vm)
        {
            if (!ModelState.IsValid)
            {
                // repopulate dropdown
                var orgs = await _orgsService.ListAsync();
                vm.Organizations = orgs.Select(o => new SelectListItem
                {
                    Value = o.OrganizationId.ToString(),
                    Text = string.IsNullOrWhiteSpace(o.Name) ? (o.NameEn ?? "Organization") : o.Name
                }).ToList();
                return View(vm);
            }

            try
            {
                var orgId = vm.OrganizationId;
                var userId = GetUserId();

                // When an Admin creates an event, the NameIdentifier claim belongs to PlatformAdmin (not Users table).
                // To satisfy FK (Event.CreatedById -> Users.UserId), map CreatedById to any active user in the same organization.
                if (User.IsInRole("Admin"))
                {
                    try
                    {
                        var allUsers = await _usersService.ListAsync();
                        var orgUser = allUsers.FirstOrDefault(u => u.OrganizationId == orgId && u.IsActive)
                                     ?? allUsers.FirstOrDefault(u => u.OrganizationId == orgId);
                        if (orgUser != null)
                        {
                            userId = orgUser.UserId;
                        }
                        else
                        {
                            // No user exists in this org yet → create a lightweight system user to satisfy FK
                            var systemUser = await _usersService.CreateAsync(new UserDto
                            {
                                UserId = Guid.NewGuid(),
                                OrganizationId = orgId,
                                FullName = "System Event Creator",
                                Email = $"system+{orgId}@mina.local",
                                // Use a unique, non-conflicting phone to avoid test/user collisions
                                Phone = $"000{orgId.ToString().Replace("-", string.Empty).Substring(0, 10)}",
                                RoleName = "Organizer",
                                ProfilePicture = string.Empty,
                                IsActive = true,
                                CreatedAt = DateTime.UtcNow
                            });
                            userId = systemUser.UserId;
                        }
                    }
                    catch { /* fallback to original userId if listing/creation fails */ }
                }

                // Validation
                if (vm.EndAt <= vm.StartAt)
                {
                    var orgs = await _orgsService.ListAsync();
                    vm.Organizations = orgs.Select(o => new SelectListItem
                    {
                        Value = o.OrganizationId.ToString(),
                        Text = string.IsNullOrWhiteSpace(o.Name) ? (o.NameEn ?? "Organization") : o.Name
                    }).ToList();
                    ModelState.AddModelError(nameof(vm.EndAt), "تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
                    return View(vm);
                }

                if (orgId == Guid.Empty)
                {
                    var orgs = await _orgsService.ListAsync();
                    vm.Organizations = orgs.Select(o => new SelectListItem
                    {
                        Value = o.OrganizationId.ToString(),
                        Text = string.IsNullOrWhiteSpace(o.Name) ? (o.NameEn ?? "Organization") : o.Name
                    }).ToList();
                    ModelState.AddModelError(nameof(vm.OrganizationId), "الجهة مطلوبة");
                    return View(vm);
                }

                var dto = new EventDto
                {
                    OrganizationId = orgId,
                    CreatedById = userId,
                    Title = vm.Title.Trim(),
                    Description = vm.Description?.Trim() ?? string.Empty,
                    StartAt = vm.StartAt,
                    EndAt = vm.EndAt,
                    RequireSignature = vm.RequireSignature,
                    StatusName = vm.Status.ToString()
                };

                var createdEvent = await _eventsService.CreateEventAsync(dto);

                // Persist builder components if provided (sections/surveys/discussions/tables/attachments)
                if (!string.IsNullOrWhiteSpace(vm.BuilderJson))
                {
                    try
                    {
                        var payload = JsonSerializer.Deserialize<BuilderPayload>(vm.BuilderJson!, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        if (payload != null)
                        {
                            await PersistBuilderAsync(createdEvent.EventId, payload);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "تعذر معالجة مكونات الإنشاء للحدث {EventId}", createdEvent.EventId);
                    }
                }

                TempData["Success"] = "تم إنشاء الحدث بنجاح";
                return RedirectToAction(nameof(Details), new { id = createdEvent.EventId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في إنشاء الحدث");
                ModelState.AddModelError("", "حدث خطأ أثناء إنشاء الحدث");
                return View(vm);
            }
        }

        // ============================================
        // GET: Admin/Events/Edit/5
        // ============================================
        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            try
            {
                var orgId = GetOrganizationId();
                var eventDto = await _eventsService.GetEventByIdAsync(id);

                if (eventDto == null)
                {
                    TempData["Error"] = "الحدث غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                // اسمح للمشرف (Admin) بالتحرير بغض النظر عن الجهة
                if (!User.IsInRole("Admin") && eventDto.OrganizationId != orgId)
                {
                    return Forbid();
                }

                var vm = new EditEventViewModel
                {
                    EventId = eventDto.EventId,
                    Title = eventDto.Title,
                    Description = eventDto.Description,
                    StartAt = eventDto.StartAt,
                    EndAt = eventDto.EndAt,
                    RequireSignature = eventDto.RequireSignature,
                    Status = Enum.TryParse<EventStatus>(eventDto.StatusName, out var status) ? status : EventStatus.Draft
                };

                return View(vm);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحميل صفحة التعديل للحدث {EventId}", id);
                TempData["Error"] = "حدث خطأ أثناء تحميل الحدث";
                return RedirectToAction(nameof(Index));
            }
        }

        // ============================================
        // POST: Admin/Events/Edit/5
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(Guid id, EditEventViewModel vm)
        {
            if (id != vm.EventId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            try
            {
                var orgId = GetOrganizationId();
                var eventDto = await _eventsService.GetEventByIdAsync(id);

                if (eventDto == null)
                {
                    TempData["Error"] = "الحدث غير موجود";
                    return RedirectToAction(nameof(Index));
                }

                // اسمح للمشرف (Admin) بالحفظ بغض النظر عن الجهة
                if (!User.IsInRole("Admin") && eventDto.OrganizationId != orgId)
                {
                    return Forbid();
                }

                // Validation
                if (vm.EndAt <= vm.StartAt)
                {
                    ModelState.AddModelError(nameof(vm.EndAt), "تاريخ النهاية يجب أن يكون بعد تاريخ البداية");
                    return View(vm);
                }

                // Update
                eventDto.Title = vm.Title.Trim();
                eventDto.Description = vm.Description?.Trim() ?? string.Empty;
                eventDto.StartAt = vm.StartAt;
                eventDto.EndAt = vm.EndAt;
                eventDto.RequireSignature = vm.RequireSignature;

                var success = await _eventsService.UpdateEventAsync(eventDto);

                if (success)
                {
                    TempData["Success"] = "تم تحديث الحدث بنجاح";
                    return RedirectToAction(nameof(Details), new { id });
                }
                else
                {
                    ModelState.AddModelError("", "فشل تحديث الحدث");
                    return View(vm);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في تحديث الحدث {EventId}", id);
                ModelState.AddModelError("", "حدث خطأ أثناء تحديث الحدث");
                return View(vm);
            }
        }

        // ============================================
        // POST: Admin/Events/Delete/5
        // ============================================
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(Guid id)
        {
            try
            {
                var orgId = GetOrganizationId();
                var eventDto = await _eventsService.GetEventByIdAsync(id);

                if (eventDto == null)
                {
                    return Json(new { success = false, message = "الحدث غير موجود" });
                }

                if (!User.IsInRole("Admin") && eventDto.OrganizationId != orgId)
                {
                    return Json(new { success = false, message = "غير مصرح لك بحذف هذا الحدث" });
                }

                var success = await _eventsService.DeleteEventAsync(id);

                if (success)

                {
                    return Json(new { success = true, message = "تم حذف حدثك بنجاح" });
                }
                else
                {
                    return Json(new { success = false, message = "فشل حذف الحدث" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطأ في حذف الحدث {EventId}", id);
                return Json(new { success = false, message = "حدث خطأ أثناء حذف الحدث" });
            }
        }

        // ============================================
        // Builder persistence helpers
        // ============================================
        private async Task PersistBuilderAsync(Guid eventId, BuilderPayload payload)
        {
            // Sections + Decisions + Items
            if (payload.Sections != null)
            {
                foreach (var s in payload.Sections)
                {
                    if (string.IsNullOrWhiteSpace(s?.Title)) continue;
                    var createdSec = await _sectionsService.CreateSectionAsync(new SectionDto
                    {
                        EventId = eventId,
                        Title = s!.Title!.Trim(),
                        Body = s.Body?.Trim() ?? string.Empty
                    });
                    if (s.Decisions != null)
                    {
                        foreach (var d in s.Decisions)
                        {
                            if (string.IsNullOrWhiteSpace(d?.Title)) continue;
                            var createdDec = await _sectionsService.AddDecisionAsync(new DecisionDto
                            {
                                SectionId = createdSec.SectionId,
                                Title = d!.Title!.Trim()
                            });
                            if (d.Items != null)
                            {
                                foreach (var it in d.Items)
                                {
                                    var text = (it ?? string.Empty).Trim();
                                    if (string.IsNullOrWhiteSpace(text)) continue;
                                    await _sectionsService.AddDecisionItemAsync(new DecisionItemDto
                                    {
                                        DecisionId = createdDec.DecisionId,
        								Text = text
                                    });
                                }
                            }
                        }
                    }

                        // Section-level components from builder
                        if (s.Surveys != null)
                        {
                            foreach (var sv in s.Surveys)
                            {
                                if (string.IsNullOrWhiteSpace(sv?.Title)) continue;
                                var createdSurvey = await _surveysService.CreateSurveyAsync(new SurveyDto
                                {
                                    EventId = eventId,
                                    SectionId = createdSec.SectionId,
                                    Title = sv!.Title!.Trim(),
                                    IsActive = true
                                });
                                if (sv.Questions != null)
                                {
                                    foreach (var q in sv.Questions)
                                    {
                                        if (string.IsNullOrWhiteSpace(q?.Text)) continue;
                                        var typeName = (q!.Type == 1) ? "Multiple" : "Single";
                                        var createdQ = await _surveysService.AddQuestionAsync(new SurveyQuestionDto
                                        {
                                            SurveyId = createdSurvey.SurveyId,
                                            Text = q.Text!.Trim(),
                                            Type = typeName,
                                            IsRequired = false
                                        });
                                        if (q.Options != null)
                                        {
                                            foreach (var opt in q.Options)
                                            {
                                                var t = (opt ?? string.Empty).Trim();
                                                if (string.IsNullOrWhiteSpace(t)) continue;
                                                await _surveysService.AddOptionAsync(new SurveyOptionDto
                                                {
                                                    QuestionId = createdQ.SurveyQuestionId,
                                                    Text = t
                                                });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        if (s.Discussions != null)
                        {
                            foreach (var d in s.Discussions)
                            {
                                if (string.IsNullOrWhiteSpace(d?.Title)) continue;
                                await _discussionsService.CreateDiscussionAsync(new DiscussionDto
                                {
                                    EventId = eventId,
                                    SectionId = createdSec.SectionId,
                                    Title = d!.Title!.Trim(),
                                    Purpose = d.Purpose?.Trim() ?? string.Empty,
                                    IsActive = true
                                });
                            }
                        }
                        if (s.Tables != null)
                        {
                            foreach (var t in s.Tables)
                            {
                                if (string.IsNullOrWhiteSpace(t?.Title)) continue;
                                var table = new TableBlockDto
                                {
                                    EventId = eventId,
                                    SectionId = createdSec.SectionId,
                                    Title = t!.Title!.Trim()
                                };
                                if (!string.IsNullOrWhiteSpace(t.RowsJson))
                                {
                                    try
                                    {
                                        using var doc = JsonDocument.Parse(t.RowsJson);
                                        if (doc.RootElement.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
                                        {
                                            var rows = new List<TableRowDto>();
                                            foreach (var rowEl in rowsEl.EnumerateArray())
                                            {
                                                var row = new TableRowDto();
                                                if (rowEl.ValueKind == JsonValueKind.Array)
                                                {
                                                    foreach (var cellEl in rowEl.EnumerateArray())
                                                    {
                                                        string val = string.Empty;
                                                        if (cellEl.ValueKind == JsonValueKind.Object && cellEl.TryGetProperty("value", out var v))
                                                            val = v.GetString() ?? string.Empty;
                                                        row.Cells.Add(val);
                                                    }
                                                }
                                                rows.Add(row);
                                            }
                                            table.TableData = new TableDataDto { Rows = rows };
                                        }
                                    }
                                    catch { /* ignore malformed json */ }
                                }
                                await _tablesService.CreateTableAsync(table);
                            }
                        }
                        if (s.Images != null)
                        {
                            foreach (var dataUrl in s.Images)
                            {
                                var bytes = TryDecodeDataUrl(dataUrl, out var fileName, out var type);
                                if (bytes != null)
                                {
                                    await _attachmentsService.UploadAttachmentAsync(new UploadAttachmentRequest
                                    {
                                        EventId = eventId,
                                        SectionId = createdSec.SectionId,
                                        FileName = fileName ?? (type == "Pdf" ? "file.pdf" : "image.png"),
                                        Type = type ?? "Image",
                                        FileData = bytes
                                    });
                                }
                            }
                        }
                        if (s.Pdfs != null)
                        {
                            foreach (var dataUrl in s.Pdfs)
                            {
                                var bytes = TryDecodeDataUrl(dataUrl, out var fileName, out var type);
                                if (bytes != null)
                                {
                                    await _attachmentsService.UploadAttachmentAsync(new UploadAttachmentRequest
                                    {
                                        EventId = eventId,
                                        SectionId = createdSec.SectionId,
                                        FileName = fileName ?? "file.pdf",
                                        Type = "Pdf",
                                        FileData = bytes
                                    });
                                }
                            }
                        }

                }
            }

            // Surveys + Questions + Options
            if (payload.Surveys != null)
            {
                foreach (var sv in payload.Surveys)
                {
                    if (string.IsNullOrWhiteSpace(sv?.Title)) continue;
                    var createdSurvey = await _surveysService.CreateSurveyAsync(new SurveyDto
                    {
                        EventId = eventId,
                        Title = sv!.Title!.Trim(),
                        IsActive = true
                    });
                    if (sv.Questions != null)
                    {
                        foreach (var q in sv.Questions)
                        {
                            if (string.IsNullOrWhiteSpace(q?.Text)) continue;
                            var typeName = (q!.Type == 1) ? "Multiple" : "Single";
                            var createdQ = await _surveysService.AddQuestionAsync(new SurveyQuestionDto
                            {
                                SurveyId = createdSurvey.SurveyId,
                                Text = q.Text!.Trim(),
                                Type = typeName,
                                IsRequired = false
                            });
                            if (q.Options != null)
                            {
                                foreach (var opt in q.Options)
                                {
                                    var t = (opt ?? string.Empty).Trim();
                                    if (string.IsNullOrWhiteSpace(t)) continue;
                                    await _surveysService.AddOptionAsync(new SurveyOptionDto
                                    {
                                        QuestionId = createdQ.SurveyQuestionId,
                                        Text = t
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // Discussions
            if (payload.Discussions != null)
            {
                foreach (var d in payload.Discussions)
                {
                    if (string.IsNullOrWhiteSpace(d?.Title)) continue;
                    await _discussionsService.CreateDiscussionAsync(new DiscussionDto
                    {
                        EventId = eventId,
                        Title = d!.Title!.Trim(),
                        Purpose = d.Purpose?.Trim() ?? string.Empty,
                        IsActive = true
                    });
                }
            }

            // Tables
            if (payload.Tables != null)
            {
                foreach (var t in payload.Tables)
                {
                    if (string.IsNullOrWhiteSpace(t?.Title)) continue;
                    var table = new TableBlockDto
                    {
                        EventId = eventId,
                        Title = t!.Title!.Trim()
                    };
                    // Optionally parse rowsJson into TableDataDto (rows -> cells)
                    if (!string.IsNullOrWhiteSpace(t.RowsJson))
                    {
                        try
                        {
                            using var doc = JsonDocument.Parse(t.RowsJson);
                            if (doc.RootElement.TryGetProperty("rows", out var rowsEl) && rowsEl.ValueKind == JsonValueKind.Array)
                            {
                                var rows = new List<TableRowDto>();
                                foreach (var rowEl in rowsEl.EnumerateArray())
                                {
                                    var row = new TableRowDto();
                                    if (rowEl.ValueKind == JsonValueKind.Array)
                                    {
                                        foreach (var cellEl in rowEl.EnumerateArray())
                                        {
                                            string val = string.Empty;
                                            if (cellEl.ValueKind == JsonValueKind.Object && cellEl.TryGetProperty("value", out var v))
                                                val = v.GetString() ?? string.Empty;
                                            row.Cells.Add(val);
                                        }
                                    }
                                    rows.Add(row);
                                }
                                table.TableData = new TableDataDto { Rows = rows };
                            }
                        }
                        catch { /* ignore malformed json */ }
                    }
                    await _tablesService.CreateTableAsync(table);
                }
            }

            // Attachments (Images / PDFs) - base64 data URLs
            if (payload.Images != null)
            {
                foreach (var dataUrl in payload.Images)
                {
                    var bytes = TryDecodeDataUrl(dataUrl, out var fileName, out var type);
                    if (bytes != null)
                    {
                        await _attachmentsService.UploadAttachmentAsync(new UploadAttachmentRequest
                        {
                            EventId = eventId,
                            FileName = fileName ?? (type == "Pdf" ? "file.pdf" : "image.png"),
                            Type = type ?? "Image",
                            FileData = bytes
                        });
                    }
                }
            }
            if (payload.Pdfs != null)
            {
                foreach (var dataUrl in payload.Pdfs)
                {
                    var bytes = TryDecodeDataUrl(dataUrl, out var fileName, out var type);
                    if (bytes != null)
                    {
                        await _attachmentsService.UploadAttachmentAsync(new UploadAttachmentRequest
                        {
                            EventId = eventId,
                            FileName = fileName ?? "file.pdf",
                            Type = "Pdf",
                            FileData = bytes
                        });
                    }
                }
            }
        }

        private static byte[]? TryDecodeDataUrl(string? dataUrl, out string? fileName, out string? type)
        {
            fileName = null; type = null;
            if (string.IsNullOrWhiteSpace(dataUrl)) return null;
            try
            {
                var parts = dataUrl.Split(',');
                if (parts.Length != 2) return null;
                var meta = parts[0];
                var base64 = parts[1];
                if (meta.Contains("pdf")) type = "Pdf"; else type = "Image";
                // Try to infer extension from meta
                if (type == "Pdf") fileName = "upload.pdf"; else if (meta.Contains("png")) fileName = "upload.png"; else if (meta.Contains("jpeg")) fileName = "upload.jpg"; else fileName = "upload.png";
                return Convert.FromBase64String(base64);
            }
            catch { return null; }
        }

        // Payload models
        private class BuilderPayload
        {
            public List<SectionPayload>? Sections { get; set; }
            public List<SurveyPayload>? Surveys { get; set; }
            public List<DiscussionPayload>? Discussions { get; set; }
            public List<TablePayload>? Tables { get; set; }
            public List<string>? Images { get; set; }
            public List<string>? Pdfs { get; set; }
        }
        private class SectionPayload
        {
            public string? Title { get; set; }
            public string? Body { get; set; }
            public List<DecisionPayload>? Decisions { get; set; }
            // New: section-level components added via builder
            public List<SurveyPayload>? Surveys { get; set; }
            public List<DiscussionPayload>? Discussions { get; set; }
            public List<TablePayload>? Tables { get; set; }
            public List<string>? Images { get; set; }
            public List<string>? Pdfs { get; set; }
        }
        private class DecisionPayload { public string? Title { get; set; } public List<string>? Items { get; set; } }
        private class SurveyPayload { public string? Title { get; set; } public List<QuestionPayload>? Questions { get; set; } }
        private class QuestionPayload { public string? Text { get; set; } public int Type { get; set; } public List<string>? Options { get; set; } }
        private class DiscussionPayload { public string? Title { get; set; } public string? Purpose { get; set; } }
        private class TablePayload { public string? Title { get; set; } public string? RowsJson { get; set; } }

        // ============================================
        // Helper Methods
        // ============================================
        private Guid GetOrganizationId()
        {
            var orgIdClaim = User.FindFirstValue("OrganizationId");
            return Guid.TryParse(orgIdClaim, out var orgId) ? orgId : Guid.Empty;
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }
    }
}

