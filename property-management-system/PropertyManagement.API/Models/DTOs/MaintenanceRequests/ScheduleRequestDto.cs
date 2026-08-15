using System;
using System.ComponentModel.DataAnnotations;

namespace PropertyManagement.API.Models.DTOs.MaintenanceRequests
{
    public class ScheduleRequestDto
    {
        [Required]
        public DateTime ScheduledDate { get; set; }
    }
}
