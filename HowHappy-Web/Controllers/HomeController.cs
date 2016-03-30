using HowHappy_Web.Models;
using HowHappy_Web.ViewModels;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Mvc.Rendering;
using Microsoft.Extensions.PlatformAbstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace HowHappy_Web.Controllers
{
    public class HomeController : Controller
    {
        //_apiKey: Replace this with your own Project Oxford Emotion API key, please do not use my key. I include it here so you can get up and running quickly but you can get your own key for free at https://www.projectoxford.ai/emotion 
        public const string _apiKey = "1dd1f4e23a5743139399788aa30a7153";

        //_apiUrl: The base URL for the API. Find out what this is for other APIs via the API documentation
        public const string _apiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        IApplicationEnvironment hostingEnvironment;
        public HomeController(IApplicationEnvironment _hostingEnvironment)
        {
            hostingEnvironment = _hostingEnvironment;
        }


        public IActionResult Index()
        {
            return View();
        }

        // POST: Home/FileExample
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Result(IFormFile file, string emotion = "happiness")
        {

            //get data and store in session
            if (file != null)
            {
                //get emotion data from api and store it in session
                var emotionDataString = await GetEmotionData(file);
                HttpContext.Session.Set("emotiondata", StringToBytes(emotionDataString));

                //get bytes from image stream and convert to a base 64 string with the required image src prefix. Also get image dimensions
                int imageHeight;
                int imageWidth;
                var base64Image = "data:image/png;base64," + FileToBase64String(file,out imageHeight,out imageWidth);

                //store base 64 image itself and image dimensions in session
                HttpContext.Session.Set("image", StringToBytes(base64Image));
                HttpContext.Session.Set("imageHeight", StringToBytes(imageHeight.ToString()));
                HttpContext.Session.Set("imageWidth", StringToBytes(imageWidth.ToString()));
            }

            //get faces list
            var emotionData = ReadSessionData("emotiondata");
            var faces = GetFaces(emotionData);

            //make calculations based on current emotion
            var themeColour = string.Empty;
            var emotionClass = string.Empty;
            var facesSorted = new List<Face>();
            switch (emotion)
            {
                case "happiness":
                    themeColour = "FFEA0E";
                    emotionClass = "fa-smile-o";
                    facesSorted = faces.OrderByDescending(o => o.scores.happiness).ToList();
                    break;
                case "anger":
                    themeColour = "FF0000";
                    emotionClass = "fa-frown-o";
                    facesSorted = faces.OrderByDescending(o => o.scores.anger).ToList();
                    break;
                case "contempt":
                    themeColour = "D3D3D3";
                    emotionClass = "fa-minus";
                    facesSorted = faces.OrderByDescending(o => o.scores.contempt).ToList();
                    break;
                case "disgust":
                    themeColour = "32CD32";
                    emotionClass = "fa-thumbs-o-down";
                    facesSorted = faces.OrderByDescending(o => o.scores.disgust).ToList();
                    break;
                case "fear":
                    themeColour = "808080";
                    emotionClass = "fa-thumbs-o-down";
                    facesSorted = faces.OrderByDescending(o => o.scores.fear).ToList();
                    break;
                case "neutral":
                    themeColour = "F5F5DC";
                    emotionClass = "fa-question";
                    facesSorted = faces.OrderByDescending(o => o.scores.neutral).ToList();
                    break;
                case "sadness":
                    themeColour = "778BFB";
                    emotionClass = "fa-frown-o";
                    facesSorted = faces.OrderByDescending(o => o.scores.sadness).ToList();
                    break;
                case "surprise":
                    themeColour = "FFA500";
                    emotionClass = "fa-smile-o";
                    facesSorted = faces.OrderByDescending(o => o.scores.surprise).ToList();
                    break;
            }

            //create view model
            var vm = new ResultViewModel()
            {
                Faces = facesSorted,
                ImagePath = ReadSessionData("image"),
                ImageHeight = int.Parse(ReadSessionData("imageHeight")),
                ImageWidth = int.Parse(ReadSessionData("imageWidth")),
                Emotion = emotion,
                Emotions = GetEmotionSelectList(),
                ThemeColour = themeColour,
                FAEmotionClass = emotionClass
            };

            //return view
            return View(vm);
        }

        public IActionResult Error()
        {
            return View();
        }

        private SelectList GetEmotionSelectList()
        {
            var emotionsList = new List<Emotion>();
            emotionsList.Add(new Emotion() { Key = "anger", Label = "Angry" });
            emotionsList.Add(new Emotion() { Key = "contempt", Label = "Contemptuous" });
            emotionsList.Add(new Emotion() { Key = "disgust", Label = "Disgusted" });
            emotionsList.Add(new Emotion() { Key = "fear", Label = "Fearful" });
            emotionsList.Add(new Emotion() { Key = "happiness", Label = "Happy" });
            emotionsList.Add(new Emotion() { Key = "neutral", Label = "Neutral" });
            emotionsList.Add(new Emotion() { Key = "sadness", Label = "Sad" });
            emotionsList.Add(new Emotion() { Key = "surprise", Label = "Surprised" });
            var emotionsSelectList = new SelectList(emotionsList, "Key", "Label");
            return emotionsSelectList;
        }

        private string ReadSessionData(string key)
        {
            byte[] bytes;
            HttpContext.Session.TryGetValue(key, out bytes);
            return BytesToString(bytes);
        }

        static byte[] StringToBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        static string BytesToString(byte[] bytes)
        {
            char[] chars = new char[bytes.Length / sizeof(char)];
            System.Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
            return new string(chars);
        }

        private List<Face> GetFaces(string json)
        {
            var faces = new List<Face>();
            
            //parse json string to object and enumerate
            var responseArray = JArray.Parse(json);
            foreach (var faceResponse in responseArray)
            {
                //deserialise json to face
                var face = JsonConvert.DeserializeObject<Face>(faceResponse.ToString());

                //add display scores
                face = AddDisplayScores(face);

                //add face to faces list
                faces.Add(face);
            }
            
            return faces;
        }

        private async Task<string> GetEmotionData(IFormFile file)
        {
            var responseString = string.Empty;

            //call emotion api
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                httpClient.BaseAddress = new Uri(_apiUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
                var content = new StreamContent(file.OpenReadStream());
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //make request
                var responseMessage = await httpClient.PostAsync(_apiUrl, content);

                //read response as a json string
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            return responseString;
        }

        private Face AddDisplayScores(Face face)
        {
            face.scores.angerDisplay = Math.Round(face.scores.anger, 2);
            face.scores.contemptDisplay = Math.Round(face.scores.contempt, 2);
            face.scores.disgustDisplay = Math.Round(face.scores.disgust, 2);
            face.scores.fearDisplay = Math.Round(face.scores.fear, 2);
            face.scores.happinessDisplay = Math.Round(face.scores.happiness, 2);
            face.scores.neutralDisplay = Math.Round(face.scores.neutral, 2);
            face.scores.sadnessDisplay = Math.Round(face.scores.sadness, 2);
            face.scores.surpriseDisplay = Math.Round(face.scores.surprise, 2);
            return face;
        }

        private string FileToBase64String(IFormFile file,out int imageHeight, out int imageWidth)
        {
            var base64String = string.Empty;
            using (var sourceStream = file.OpenReadStream())
            {
                using (var sourceMemoryStream = new MemoryStream())
                {
                    sourceStream.CopyTo(sourceMemoryStream);
                    var image = System.Drawing.Image.FromStream(sourceMemoryStream);
                    imageHeight = image.Height;
                    imageWidth = image.Width;
                    var bytes = sourceMemoryStream.ToArray();
                    base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
                }
            }
            return base64String;
        }

    }
}
