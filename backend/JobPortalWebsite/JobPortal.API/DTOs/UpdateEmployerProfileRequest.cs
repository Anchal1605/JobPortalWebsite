namespace JobPortal.API.DTOs
{
    public class UpdateEmployerProfileRequest
    {
        public string? Name { get; set; }
        public string? Designation { get; set; }
        public string? ContactNumber { get; set; }
        public string? CompanyName { get; set; }
        public string? CompanyWebsite { get; set; }
        public string? CompanyLocation { get; set; }
        public string? CompanyDescription { get; set; }
        public string? CompanyLogoUrl { get; set; }
    }
}
