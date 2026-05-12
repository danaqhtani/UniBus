namespace UniBus.Models
{
    public class ProfileViewModel
    {
        // Real student data from database
        public int StudentId { get; set; }
        public string FullName { get; set; }
        public string Username { get; set; }
        public string UniversityEmail { get; set; }
        public string PhoneNumber { get; set; }
        public int BuildingId { get; set; }

        // Friendly UI value
        public string CampusLabel { get; set; }
    }
}