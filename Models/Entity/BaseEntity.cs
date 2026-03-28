namespace Rapsodia.Models.Entity
{
    // Campos de auditoria herdados por todas as entidades principais.
    public abstract class BaseEntity
    {
        public int       Id        { get; set; }
        public DateTime  CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
    }
}
