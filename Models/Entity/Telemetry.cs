using System;
using System.ComponentModel.DataAnnotations;

namespace Rapsodia.Models.Entity
{
    public class Telemetry : BaseEntity
    {
        [Required]
        public Guid AgentId { get; set; }

        [Required]
        [MaxLength(100)]
        public string SessionId { get; set; } = string.Empty;

        [MaxLength(500)]
        public string TargetFilePath { get; set; } = string.Empty;

        [Required]
        public double EntropyValue { get; set; }

        [Required]
        public DateTime AnalysisTimestamp { get; set; }

        public long ProcessingTimeMs { get; set; }

        [Required]
        [MaxLength(50)]
        public string RiskLevel { get; set; } = string.Empty;
    }
}
