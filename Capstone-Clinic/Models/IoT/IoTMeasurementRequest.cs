using System;
using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models.IoT
{
    public class IoTMeasurementRequest
    {
        [Key]
        public Guid RequestId { get; set; }

        [Required]
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; }

        public DateTime ExpiresAt { get; set; }

        public DateTime? ClaimedAt { get; set; }

        public DateTime? CompletedAt { get; set; }
    }
}