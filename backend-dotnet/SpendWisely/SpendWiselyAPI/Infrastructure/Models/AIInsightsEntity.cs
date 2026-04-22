using System.ComponentModel.DataAnnotations.Schema;

namespace SpendWiselyAPI.Infrastructure.Models
{
    public class AIInsightsEntity
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public string Insights { get; set; } // JSON from OpenAI
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Navigation property (optional)
        [ForeignKey("UserId")]
        public virtual UserEntity User { get; set; } // Navigation for user-specific
    }
}
