using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models
{
    public class AgentIntegration
    {
        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [MaxLength(64)]
        public String Platform { get; set; }

        [MaxLength(64)]
        public String VerifyToken { get; set; }

        [MaxLength(256)]
        public String AccessToken { get; set; }
    }
}
