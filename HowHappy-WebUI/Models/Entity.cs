using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HowHappy.Models
{
    public class Entity
    {
        public string entity { get; set; }
        public string type { get; set; }
        public int startIndex { get; set; }
        public int endIndex { get; set; }
        public double score { get; set; }
    }
}
