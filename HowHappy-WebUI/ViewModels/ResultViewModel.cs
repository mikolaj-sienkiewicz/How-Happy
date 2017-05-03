using HowHappy.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace HowHappy.ViewModels
{
    public class ResultViewModel
    {
        public  List<Face> Faces { get; set; }

        public string Emotion { get; set; }

        public double AverageEmotionScore { get; set; }

        public string Intent { get; set; }

        public int Ordinal { get; set; }

        public string LuisQuery { get; set; }

        public SelectList Emotions { get; set; }

        public string ThemeColour { get; set; }

        public string FAEmotionClass { get; set; }
    }
}
