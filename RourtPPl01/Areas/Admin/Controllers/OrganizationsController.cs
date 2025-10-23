using EventPl.Dto;
using EventPl.Services.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EventPresentationlayer.ViewModels;

namespace RourtPPl01.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "PlatformAdmin")]
    public class OrganizationsController : Controller
    {
        private readonly ICrudService<OrganizationDto, Guid> _organizations;
        private readonly ILogger<OrganizationsController> _logger;

        public OrganizationsController(ICrudService<OrganizationDto, Guid> organizations, ILogger<OrganizationsController> logger)
        {
            _organizations = organizations;
            _logger = logger;
        }


        [HttpGet]
        public IActionResult Index()
        {
            // Redirect legacy Organizations route to Groups
            return RedirectToAction("Index", "Groups", new { area = "Admin" });
        }

        [HttpGet]
        public IActionResult Create()
        {
            return RedirectToAction("Create", "Groups", new { area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(OrganizationFormVm vm)
        {
            return RedirectToAction("Create", "Groups", new { area = "Admin" });
        }

        [HttpGet]
        public IActionResult Edit(Guid id)
        {
            return RedirectToAction("Edit", "Groups", new { id, area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(Guid id, OrganizationFormVm vm)
        {
            return RedirectToAction("Edit", "Groups", new { id, area = "Admin" });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(Guid id)
        {
            return RedirectToAction("Delete", "Groups", new { id, area = "Admin" });
        }

    }
}

