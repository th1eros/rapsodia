using System;

namespace API_SVsharp.DTO.TelemetryDTO
{
    public class TelemetryResponseDTO
    {
        public int Id { get; set; }
        public Guid AgentId { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public string TargetFilePath { get; set; } = string.Empty;
        public double EntropyValue { get; set; }
        public DateTime AnalysisTimestamp { get; set; }
        public long ProcessingTimeMs { get; set; }
        public string RiskLevel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
    }
}
