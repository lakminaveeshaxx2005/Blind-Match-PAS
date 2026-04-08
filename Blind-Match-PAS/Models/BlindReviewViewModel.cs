using System.Collections.Generic;

namespace Blind_Match_PAS.Models
{
    /// <summary>
    /// ViewModel for Blind Review page - displays matching requests anonymously
    /// without revealing student personal information
    /// </summary>
    public class BlindReviewViewModel
    {
        /// <summary>
        /// List of blind review items for pending requests
        /// </summary>
        public List<BlindReviewItem> ReviewItems { get; set; } = new List<BlindReviewItem>();
    }

    /// <summary>
    /// Individual item for blind review display
    /// Contains only project details and match score, no student info
    /// </summary>
    public class BlindReviewItem
    {
        /// <summary>
        /// Request ID for approve/reject actions
        /// </summary>
        public int RequestId { get; set; }

        /// <summary>
        /// Project title
        /// </summary>
        public string ProjectTitle { get; set; } = string.Empty;

        /// <summary>
        /// Project abstract/description
        /// </summary>
        public string ProjectAbstract { get; set; } = string.Empty;

        /// <summary>
        /// Tech stack used in the project
        /// </summary>
        public string TechStack { get; set; } = string.Empty;

        /// <summary>
        /// Research areas
        /// </summary>
        public string ResearchAreas { get; set; } = string.Empty;

        /// <summary>
        /// Calculated match score between student and supervisor
        /// </summary>
        public int MatchScore { get; set; }
    }
}