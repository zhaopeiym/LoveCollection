using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace LoveCollection.Dto
{
    public class CollectionOutput
    {
        public string Title { get; set; }
        public string Url { get; set; }
        public double Sort { get;  set; }
        public int Id { get; set; }
        public int TypeId { get; set; }
    }
}
