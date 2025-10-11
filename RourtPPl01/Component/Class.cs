using EventPl.Services.Interface;
using Microsoft.AspNetCore.Mvc;

namespace EventPresentationlayer.Components
{
    public class EventResultsViewComponent : ViewComponent
    {
        private readonly IEventResultsService _svc;
        public EventResultsViewComponent(IEventResultsService svc) => _svc = svc;

        public async Task<IViewComponentResult> InvokeAsync(Guid eventId, bool isAdmin = true)
        {
            var vm = isAdmin
                ? await _svc.GetAdminResultsAsync(eventId)
                : await _svc.GetUserResultsAsync(eventId);

            return View(vm); // Views/Shared/Components/EventResults/Default.cshtml
        }
    }
}
