using System.ComponentModel.DataAnnotations;

namespace sodoff.Model
{
    public class UserActivity
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int VikingId { get; set; }
        public Guid RelatedVikingUid { get; set; }
        public int VikingActivityTypeId { get; set; }
        public DateTime? LastActivityAt { get; set; }

        public virtual Viking? Viking { get; set; }
    }
}
