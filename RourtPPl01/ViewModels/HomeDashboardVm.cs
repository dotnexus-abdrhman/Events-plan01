namespace EventPresentationlayer.ViewModels
{
    public class HomeDashboardVm
    {
        public int OrganizationsCount { get; set; }
        public int EventsCount { get; set; }
        public int UsersCount { get; set; }
        public DateTime LastRefreshUtc { get; set; }
        public int DocumentsCount { get; set; }
        public int NotificationsCount { get; set; }

      
    }
}
