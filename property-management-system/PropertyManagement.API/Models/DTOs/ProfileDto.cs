namespace PropertyManagement.API.Models.DTOs
{
    public class ProfileDto
    {
        public long UserAccountId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? FullName { get; set; }
        public string? ContactNumber { get; set; }
        public string? Gender { get; set; }
        public string? ProfilePictureUrl { get; set; }
        
        // For Occupants
        public string? OccupantType { get; set; }
    }
}
