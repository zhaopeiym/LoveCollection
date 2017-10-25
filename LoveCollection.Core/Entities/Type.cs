using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LoveCollection.Core.Entities
{
    public class Type
    {
        public int Id { get; set; }

        [MaxLength(100)]
        public string Name { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public double Sort { get; set; }
    }
}
