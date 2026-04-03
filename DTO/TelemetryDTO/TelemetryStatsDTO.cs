namespace Rapsodia.DTO.TelemetryDTO
{
    public class TelemetryStatsDTO
    {
        public int TotalRecords { get; set; }
        public int CriticalCount { get; set; }
        public int HighCount { get; set; }
        public int MediumCount { get; set; }
        public int LowCount { get; set; }
        public double AverageEntropy { get; set; }
        public double MaxEntropy { get; set; }
        public double MinEntropy { get; set; }
        public long AverageProcessingMs { get; set; }
        public int UniqueAgents { get; set; }
        public int UniqueSessions { get; set; }
        public DateTime? LastEventAt { get; set; }
        // Últimas 24h agrupadas por hora (para sparkline)
        public List<HourlyBucket> HourlyDistribution { get; set; } = new();
        // Distribuição por RiskLevel
        public List<RiskBucket> RiskDistribution { get; set; } = new();
    }

    public class HourlyBucket
    {
        public string Hour { get; set; } = string.Empty; // "HH:00"
        public int Count { get; set; }
        public double AvgEntropy { get; set; }
    }

    public class RiskBucket
    {
        public string RiskLevel { get; set; } = string.Empty;
        public int Count { get; set; }
        public double Percentage { get; set; }
    }
}