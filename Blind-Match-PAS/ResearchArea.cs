using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public class ResearchArea
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string Name { get; set; }
    }
}