using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace LoveCollection.Entities
{
    public class User
    {
        public User()
        {
            LastOnlineTime = DateTime.Now;
            CreationTime = DateTime.Now;
        }
        public int Id { get; set; }
        [MaxLength(50)]
        public string Mail { get; set; }
        [MaxLength(20)]
        public string Passwod { get; set; }
        /// <summary>
        /// 最后在线时间
        /// </summary>
        public DateTime LastOnlineTime { get; set; }
        /// <summary>
        /// 创建时间
        /// </summary>
        public DateTime CreationTime { get; set; }
    }
}
