using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Dialogflow.Models
{
    public class EntrySynonym
    {
        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        [Required]
        [StringLength(36)]
        public String EntityEntryId { get; set; }

        [MaxLength(128)]
        public String Synonym { get; set; }
    }
}
