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

        public IActionResult Index2()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Result2()
        {
            //get form data
            var file = Request.Form.Files[0];
            var emotion = Request.Form["emotion"];

            //get faces list
            if (string.IsNullOrEmpty(ReadSessionData("emotiondata")))
            {
                //get emotion data from api and store it in session
                var emotionDataString = await GetEmotionData(file);
                HttpContext.Session.Set("emotiondata", StringToBytes(emotionDataString));
            }

            //get faces list
            var emotionData = ReadSessionData("emotiondata");
            var faces = GetFaces(emotionData);

            //create view model
            var vm = new Result2ViewModel()
            {
                Faces = GetSortedFacesList(faces, emotion),
                Emotion = emotion,
                Emotions = GetEmotionSelectList(),
                ThemeColour = GetThemeColour(emotion),
                FAEmotionClass = GetEmojiClass(emotion)
            };

            return Json(vm);
        }

        // POST: Home/FileExample
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Result(IFormFile file, string emotion = "happiness")
        {

            //get data and store in session. Need to store in session to avoid re-calling the emotion api when the user changes the selected emotion.
            if (file != null)
            {
                //get emotion data from api and store it in session
                var emotionDataString = await GetEmotionData(file);
                HttpContext.Session.Set("emotiondata", StringToBytes(emotionDataString));

                //get bytes from image stream and convert to a base 64 string with the required image src prefix. Also get image dimensions
                //This seems to be throwing an exception only on azure with large images. removing for now
                var dimensions = GetImageDimensions(file);
                var imageWidth = dimensions.Width;
                var imageHeight = dimensions.Height;
                var base64Image = "data:image/png;base64," + FileStreamToBase64String(file);

                //store base 64 image itself and image dimensions in session
                HttpContext.Session.Set("imageHeight", StringToBytes(imageHeight.ToString()));
                HttpContext.Session.Set("imageWidth", StringToBytes(imageWidth.ToString()));
                HttpContext.Session.Set("image", StringToBytes(base64Image));
            }

            //get faces list
            var emotionData = ReadSessionData("emotiondata");
            var faces = GetFaces(emotionData);

            //create view model
            var vm = new ResultViewModel()
            {
                Faces = GetSortedFacesList(faces, emotion),
                ImagePath = ReadSessionData("image"),
                ImageHeight = int.Parse(ReadSessionData("imageHeight")),
                ImageWidth = int.Parse(ReadSessionData("imageWidth")),
                Emotion = emotion,
                Emotions = GetEmotionSelectList(),
                ThemeColour = GetThemeColour(emotion),
                FAEmotionClass = GetEmojiClass(emotion)
            };

            //return view
            return View(vm);
        }

        private string GetThemeColour(string emotion)
        {
            switch (emotion)
            {
                case "happiness":
                    return "FFEA0E";
                case "anger":
                    return "FF0000";
                case "contempt":
                    return "D3D3D3";
                case "disgust":
                    return "32CD32";
                case "fear":
                    return "808080";
                case "neutral":
                    return "F5F5DC";
                case "sadness":
                    return "778BFB";
                case "surprise":
                    return "FFA500";
                default:
                    return "FFEA0E";
            }
        }

        private string GetEmojiClass(string emotion)
        {
            switch (emotion)
            {
                case "happiness":
                    return "fa-smile-o";
                case "anger":
                    return "fa-frown-o";
                case "contempt":
                    return "fa-minus";
                case "disgust":
                    return "fa-thumbs-o-down";
                case "fear":
                    return "fa-thumbs-o-down";
                case "neutral":
                    return "fa-question";
                case "sadness":
                    return "fa-frown-o";
                case "surprise":
                    return "fa-smile-o";
                default:
                    return "fa-smile-o";
            }
        }

        private List<Face> GetSortedFacesList(List<Face> faces, string emotion)
        {
            switch (emotion)
            {
                case "happiness":
                    return faces.OrderByDescending(o => o.scores.happiness).ToList();
                case "anger":
                    return faces.OrderByDescending(o => o.scores.anger).ToList();
                case "contempt":
                    return faces.OrderByDescending(o => o.scores.contempt).ToList();
                case "disgust":
                    return faces.OrderByDescending(o => o.scores.disgust).ToList();
                case "fear":
                    return faces.OrderByDescending(o => o.scores.fear).ToList();
                case "neutral":
                    return faces.OrderByDescending(o => o.scores.neutral).ToList();
                case "sadness":
                    return faces.OrderByDescending(o => o.scores.sadness).ToList();
                case "surprise":
                    return faces.OrderByDescending(o => o.scores.surprise).ToList();
                default:
                    return faces;
            }
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
            if (bytes == null){
                return string.Empty;
            }
            else {
                return BytesToString(bytes);
            }

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

        private string FileStreamToBase64String(IFormFile file)
        {
            string base64String;
            using (var sourceStream = file.OpenReadStream())
            {
                using (var sourceMemoryStream = new MemoryStream())
                {
                    sourceStream.CopyTo(sourceMemoryStream);
                    var bytes = sourceMemoryStream.ToArray();
                    base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
                }
            }
            return base64String;
        }

        private Dimensions GetImageDimensions(IFormFile file)
        {
            //would this be better done in javascript on the client? http://stackoverflow.com/questions/2865017/get-image-dimensions-using-javascript-during-file-upload
            Dimensions dimensions = new Dimensions();
            using (var sourceStream = file.OpenReadStream())
            {
                //this has a dependency on System.Drawing from .net 4.x, but there is not yet a good solution for image handling in asp.net core 1.0. See http://www.hanselman.com/blog/RFCServersideImageAndGraphicsProcessingWithNETCoreAndASPNET5.aspx
                using (var image = System.Drawing.Image.FromStream(sourceStream))
                {
                    dimensions.Height = image.Height;
                    dimensions.Width = image.Width;
                }
            }
            return dimensions;
        }


    }
}
