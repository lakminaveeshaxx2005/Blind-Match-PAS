using System.ComponentModel.DataAnnotations;

namespace Blind_Match_PAS.Models
{
    public class Project
    {
        [Key]
        public int Id { get; set; }

        [Required(ErrorMessage = "Title is required.")]
        [StringLength(200, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 200 characters.")]
        [Display(Name = "Project Title")]
        public required string Title { get; set; }

        [Required(ErrorMessage = "Abstract is required.")]
        [StringLength(1000, MinimumLength = 20, ErrorMessage = "Abstract must be between 20 and 1000 characters.")]
        [DataType(DataType.MultilineText)]
        [Display(Name = "Abstract")]
        public required string Abstract { get; set; }

        [Required(ErrorMessage = "Tech stack is required.")]
        [StringLength(500, MinimumLength = 3, ErrorMessage = "Tech stack must be between 3 and 500 characters.")]
        [Display(Name = "Tech Stack")]
        public required string TechStack { get; set; }

        [Required(ErrorMessage = "Research area is required.")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Research area must be between 3 and 200 characters.")]
        [Display(Name = "Research Area")]
        public required string ResearchArea { get; set; }

        [Required]
        [StringLength(450)]
        public required string StudentId { get; set; }
    }
}
