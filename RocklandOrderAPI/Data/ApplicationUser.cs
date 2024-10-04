using Microsoft.AspNetCore.Identity;

namespace RocklandOrderAPI.Data
{
    public class ApplicationUser : IdentityUser
    {
        public required string FirstName { get; set; }
        public string MiddleName { get; set; } = string.Empty;
        public required string LastName { get; set; }
        public string Suffix { get; set; } = string.Empty;
        public string CompanyName {  get; set; } = string.Empty;
        public required string Address1 {  get; set; }
        public string Address2 { get; set; } = string.Empty;
        public required string City { get; set; }
        public required string State {  get; set; }
        public required string PostalCode { get; set; }
        public string TimeZoneId { get; set; } = TimeZoneInfo.Utc.Id;
        public bool OptInAccountNotices { get; set; } = false;
        public bool OptInProductNotices { get; set; } = false;
    }
}
