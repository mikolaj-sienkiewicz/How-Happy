using HowHappy_Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HowHappy_Web.ViewModels
{
    public class ResultViewModel
    {
        public  List<FaceWithPosition> Faces { get; set; }

        public string ImagePath { get; set; }

        public byte[] Image { get; set; }
    }
}
