using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HowHappy.Models
{
    public class LuisResult
    {
        public string query { get; set; }
        public List<Intent> intents { get; set; }
        public List<Entity> entities { get; set; }
    }
}
