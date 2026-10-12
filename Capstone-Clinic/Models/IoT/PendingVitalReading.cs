using System;
using System.ComponentModel.DataAnnotations;

namespace Capstone_Clinic.Models.IoT
{
    public class PendingVitalReading
    {
        [Key]
        public int PendingVitalReadingId { get; set; }

        public int HeartRate { get; set; }

        public int Spo2 { get; set; }

        public bool FingerDetected { get; set; }

        public DateTime ReceivedAt { get; set; }

        public bool IsUsed { get; set; }
    }
}