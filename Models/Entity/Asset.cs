using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace API_SVsharp.Models.Entity
{
    public class Asset : BaseEntity
    {
        public string Nome { get; set; } = null!;
        // Ex: "Servidor Debian", "API Financeira"

        public string Type { get; set; } = null!;
        // Ex: "OperatingSystem", "WebApplication", "Database", "API"

        public string Environment { get; set; } = null!;
        // Ex: "DEV", "HML", "PROD"

        public bool Active { get; set; } = true;

        public DateTime? ArchivedAt { get; set; }

        [JsonIgnore]
        public ICollection<AssetVuln> AssetVulns { get; set; } = new List<AssetVuln>();
    }
}