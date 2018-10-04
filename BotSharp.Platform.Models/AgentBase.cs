using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BotSharp.Platform.Models
{
    public abstract class AgentBase
    {
        public AgentBase()
        {
            CreatedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        /// <summary>
        /// Name of chatbot
        /// </summary>
        [Required]
        [MaxLength(64)]
        public virtual String Name { get; set; }

        /// <summary>
        /// Description of chatbot
        /// </summary>
        [MaxLength(256)]
        public String Description { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        [MaxLength(5)]
        public String Language { get; set; }

        [Required]
        public DateTime CreatedDate { get; set; }

        public TrainingCorpus Corpus { get; set; }
    }
}
