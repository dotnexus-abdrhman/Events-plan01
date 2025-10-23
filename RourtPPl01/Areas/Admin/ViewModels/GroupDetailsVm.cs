using EventPl.Dto;
using System.Collections.Generic;

namespace RourtPPl01.Areas.Admin.ViewModels
{
    public class GroupDetailsVm
    {
        public OrganizationDto Group { get; set; } = new OrganizationDto();
        public List<UserDto> Users { get; set; } = new List<UserDto>();
        public List<EventDto> Events { get; set; } = new List<EventDto>();
    }
}

