using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace HowHappy.Models
{
    public class FaceDto
    {
        public FaceRectangleDto faceRectangle { get; set; }
        public FaceAttributesDto faceAttributes { get; set; }
    }

    public class FaceRectangleDto
    {
        public int top { get; set; }
        public int left { get; set; }
        public int width { get; set; }
        public int height { get; set; }
    }

    public class EmotionDto
    {
        public double anger { get; set; }
        public double contempt { get; set; }
        public double disgust { get; set; }
        public double fear { get; set; }
        public double happiness { get; set; }
        public double neutral { get; set; }
        public double sadness { get; set; }
        public double surprise { get; set; }
    }

    public class FaceAttributesDto
    {
        public EmotionDto emotion { get; set; }
    }
}
