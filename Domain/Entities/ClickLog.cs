using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FinanceAPI.Domain.Entities
{
    public class ClickLog
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        [Required]
        public string ProjectName { get; set; } = string.Empty;

        public string? State { get; set; }
        public string? City { get; set; }

        public DateTime ClickedAt { get; set; } = DateTime.UtcNow;
    }
}
