using HowHappy_Web.Models;
using HowHappy_Web.ViewModels;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Mvc;
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
            List<Face> faces = new List<Face>();
            string base64Image = string.Empty;

            //get faces
            faces = await GetFaces(file);

            //get bytes from image stream and convert to a base 64 string with the required image src prefix
            base64Image = "data:image/png;base64," + FileToBase64String(file);

            //sort list by happiness score
            var facesSorted = new List<Face>();
            switch (emotion)
            {
                case "happiness":
                    facesSorted = faces.OrderByDescending(o => o.scores.happiness).ToList();
                    break;
                case "anger":
                    facesSorted = faces.OrderByDescending(o => o.scores.anger).ToList();
                    break;
                case "contempt":
                    facesSorted = faces.OrderByDescending(o => o.scores.contempt).ToList();
                    break;
                case "disgust":
                    facesSorted = faces.OrderByDescending(o => o.scores.disgust).ToList();
                    break;
                case "fear":
                    facesSorted = faces.OrderByDescending(o => o.scores.fear).ToList();
                    break;
                case "neutral":
                    facesSorted = faces.OrderByDescending(o => o.scores.neutral).ToList();
                    break;
                case "sadness":
                    facesSorted = faces.OrderByDescending(o => o.scores.sadness).ToList();
                    break;
                case "surprise":
                    facesSorted = faces.OrderByDescending(o => o.scores.surprise).ToList();
                    break;
            }

            //create view model
            var vm = new ResultViewModel()
            {
                Faces = facesSorted,
                ImagePath = base64Image
            };

            //return view
            return View(vm);
        }

        public IActionResult Error()
        {
            return View();
        }

        private async Task<List<Face>> GetFaces(IFormFile file)
        {
            var faces = new List<Face>();

            //call emotion api and handle results
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
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                //parse json string to object and enumerate
                var responseArray = JArray.Parse(responseString);
                foreach (var faceResponse in responseArray)
                {
                    //deserialise json to face
                    var face = JsonConvert.DeserializeObject<Face>(faceResponse.ToString());

                    //add display scores
                    face = AddDisplayScores(face);

                    //add face to faces list
                    faces.Add(face);
                }
            }

            return faces;
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

        private string FileToBase64String(IFormFile file)
        {
            var base64String = string.Empty;
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
    }
}
