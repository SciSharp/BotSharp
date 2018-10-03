using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace Platform.Dialogflow.Models
{
    public class EntityType
    {
        /// <summary>
        /// Guid
        /// </summary>
        [StringLength(36)]
        public String Id { get; set; }

        [Required]
        [StringLength(36)]
        public String AgentId { get; set; }

        [Required]
        [MaxLength(64)]
        public String Name { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        [ForeignKey("EntityId")]
        public List<EntityEntry> Entries { get; set; }

        public bool IsOverridable { get; set; }

        public bool IsEnum { get; set; }

        [StringLength(6)]
        public string Color { get; set; }

        /// <summary>
        /// Entries count
        /// </summary>
        [NotMapped]
        public int Count { get; set; }
    }
}
