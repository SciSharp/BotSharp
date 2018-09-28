using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Platform.Models
{
    /// <summary>
    /// Standard agent data structure
    /// All other platform agent has to align with this standard data structure.
    /// </summary>
    public class StandardAgent<TExtraData, TEntity>
    {
        public StandardAgent()
        {
            CreatedDate = DateTime.UtcNow;
            Entities = new List<TEntity>();
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
        public String Name { get; set; }

        /// <summary>
        /// Description of chatbot
        /// </summary>
        [MaxLength(256)]
        public String Description { get; set; }

        /// <summary>
        /// Is the chatbot public or private
        /// </summary>
        public Boolean Published { get; set; }

        /// <summary>
        /// 
        /// </summary>
        [Required]
        [MaxLength(5)]
        public String Language { get; set; }

        /// <summary>
        /// Only access text/ audio rquest
        /// </summary>
        [StringLength(32)]
        public String ClientAccessToken { get; set; }

        /// <summary>
        /// Developer can access more APIs
        /// </summary>
        [StringLength(32)]
        public String DeveloperAccessToken { get; set; }

        //public List<Intent> Intents { get; set; }

        public List<TEntity> Entities { get; set; }

        public String Birthday
        {
            get
            {
                return CreatedDate.ToShortDateString();
            }
        }

        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// Save extra information for specific platform
        /// </summary>
        public TExtraData ExtraData { get; set; }
    }
}
