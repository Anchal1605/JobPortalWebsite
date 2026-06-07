namespace JobPortal.API.DTOs;

public class EmployerProfileDto
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public string? Designation { get; set; }
    public string? ContactNumber { get; set; }
    public CompanyDto? Company { get; set; }
}
