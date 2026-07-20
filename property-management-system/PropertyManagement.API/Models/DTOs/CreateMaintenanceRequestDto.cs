using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace PropertyManagement.API.Models.DTOs
{
    public class CreateMaintenanceRequestDto
    {
        [Required]
        [MaxLength(100)]
        public string Title { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(100)]
        public string IssueCategory { get; set; } = string.Empty;
        
        [Required]
        public string Description { get; set; } = string.Empty;
        
        [Required]
        public long UnitId { get; set; }
        
        public IFormFile? Image { get; set; }
    }
}
