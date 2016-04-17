using HowHappy_Web.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc.Rendering;

namespace HowHappy_Web.ViewModels
{
    public class TestResultViewModel
    {
        public  string FacesJson { get; set; }

        public string Emotion { get; set; }

        public SelectList Emotions { get; set; }

        public string ThemeColour { get; set; }

        public string FAEmotionClass { get; set; }
    }
}
