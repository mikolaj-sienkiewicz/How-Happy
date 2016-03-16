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
        public async Task<IActionResult> Result(IFormFile file)
        {
            var vm = new ResultViewModel();

            //get bytes from image stream and put the base 64 string in the view model
            using (var sourceStream = file.OpenReadStream())
            {
                using (var sourceMemoryStream = new MemoryStream())
                {
                    sourceStream.CopyTo(sourceMemoryStream);
                    var bytes = sourceMemoryStream.ToArray();
                    var base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
                    vm.ImagePath = "data:image/png;base64," + base64String;
                }
            }

            //call emotion api and handle results
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient
                httpClient.BaseAddress = new Uri(_apiUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _apiKey);
                httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/octet-stream"));

                //setup data object
                HttpContent content = new StreamContent(file.OpenReadStream());
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //make request
                var responseMessage = await httpClient.PostAsync(_apiUrl, content);

                //read response and write to view
                var responseString = await responseMessage.Content.ReadAsStringAsync();

                //parse json string to object and enumerate
                List<Face> faces = new List<Face>();
                JArray responseArray = JArray.Parse(responseString);
                foreach (var faceResponse in responseArray)
                {
                    //deserialise json to face
                    var face = JsonConvert.DeserializeObject<Face>(faceResponse.ToString());

                    //round scores to make them more readable
                    face.scores.anger = Math.Round(face.scores.anger, 4);
                    face.scores.contempt = Math.Round(face.scores.contempt, 4);
                    face.scores.disgust = Math.Round(face.scores.disgust, 4);
                    face.scores.fear = Math.Round(face.scores.fear, 4);
                    face.scores.happiness = Math.Round(face.scores.happiness, 6);
                    face.scores.neutral = Math.Round(face.scores.neutral, 4);
                    face.scores.sadness = Math.Round(face.scores.sadness, 4);
                    face.scores.surprise = Math.Round(face.scores.surprise, 4);

                    //add face to faces list
                    faces.Add(face);
                }

                //sort list by happiness score
                List<Face> facesSorted = faces.OrderByDescending(o => o.scores.happiness).ToList();

                //add list of faces to view model
                vm.Faces = facesSorted;
            }

            //return view
            return View(vm);
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
