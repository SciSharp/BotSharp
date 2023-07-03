using BotSharp.Core.Repository.Abstraction;
using EntityFrameworkCore.BootKit;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;

namespace BotSharp.Plugin.WeChat.Repository.DbTables
{
    [Table("WeChatAccount")]
    public class WeChatAccount : DbRecord, IAgentTable
    {
        [Required]
        [MaxLength(18)]
        public string WeChatAppId { get; set; }

        [Required]
        [MaxLength(36)]
        public string WeChatOpenId { get; set; }

        [Required]
        [MaxLength(36)]
        public string UserId { get; set; }
    }
}
