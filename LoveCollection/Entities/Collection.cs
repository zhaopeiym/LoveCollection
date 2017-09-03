using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace LoveCollection.Entities
{
    /// <summary>
    ///收藏
    /// </summary>
    public class Collection
    {
        public int Id { get; set; }
        [MaxLength(200)]
        public string Url { get; set; }
        [MaxLength(50)]
        public string Title { get; set; }


        public int TypeId { get; set; }
        /// <summary>
        /// 分类
        /// </summary>
        [ForeignKey("TypeId")]

        public virtual Type Type { get; set; }

        public int? TagId { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        [ForeignKey("TagId")]

        public virtual Tag Tag { get; set; }

        /// <summary>
        /// 排序
        /// </summary>
        public double Sort { get; set; }

        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public User User { get; set; }
    }
}
