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
        public String UserName { get; set; }

        [Required]
        [StringLength(64)]
        [DataType(DataType.EmailAddress)]
        public String Email { get; set; }

        [StringLength(32)]
        public String FirstName { get; set; }

        [StringLength(32)]
        public String LastName { get; set; }

        [MaxLength(256)]
        public String Description { get; set; }

        [Required]
        [DataType(DataType.DateTime)]
        public DateTime SignupDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime? Birthday { get; set; }

        [MaxLength(36)]
        public String Nationality { get; set; }

        [NotMapped]
        public String FullName
        {
            get
            {
                return FirstName + (String.IsNullOrEmpty(LastName) ? "" : " " + LastName);
            }
        }

        public UserAuth Authenticaiton { get; set; }

        public User()
        {
            SignupDate = DateTime.UtcNow;
        }
    }
}
