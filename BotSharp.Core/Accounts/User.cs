using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Core.Accounts
{
    /// <summary>
    /// User profile
    /// </summary>
    [Table("User")]
    public class User : DbRecord, IDbRecord
    {
        [Required]
        [StringLength(64)]
        public string UserName { get; set; }

        [Required]
        [StringLength(64)]
        [DataType(DataType.EmailAddress)]
        public string Email { get; set; }

        [StringLength(32)]
        public string FirstName { get; set; }

        [StringLength(32)]
        public string LastName { get; set; }

        [MaxLength(256)]
        public string Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime SignupDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [MaxLength(36)]
        public string Nationality { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                return FirstName + (string.IsNullOrEmpty(LastName) ? "" : " " + LastName);
            }
        }

        public UserAuth Authenticaiton { get; set; }

        public User()
        {
            SignupDate = DateTime.UtcNow;
        }
    }
}
