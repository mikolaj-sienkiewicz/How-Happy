using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using HowHappy_Web.Models;
using System.IO;
using Microsoft.Extensions.PlatformAbstractions;
using HowHappy_Web.ViewModels;

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
            using (var stream = file.OpenReadStream())
            {
                using (var memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);
                    var bytes = memoryStream.ToArray();
                    var base64String = Convert.ToBase64String(bytes, 0, bytes.Length);
                    vm.ImagePath = "data:image/png;base64," + base64String;
                }
            }

            using (var httpClient = new HttpClient())
            {
#if DEBUG
                //Use local json for testing
                var responseString = string.Format("");
                using (StreamReader reader = System.IO.File.OpenText(@"..\data\EdFullSize.json"))
                {
                    responseString = await reader.ReadToEndAsync();
                }

#else
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

#endif

                //parse json string to object 
                List<Face> faces = new List<Face>();
                JArray responseArray = JArray.Parse(responseString);
                foreach (var faceResponse in responseArray)
                {
                    var face = JsonConvert.DeserializeObject<Face>(faceResponse.ToString());
                    faces.Add(face);
                }

                //sort list
                List<Face> facesSorted = faces.OrderByDescending(o => o.scores.happiness).ToList();

                //create a new list with position
                List<FaceWithPosition> facesWithPosition = new List<FaceWithPosition>();
                var count = 1;
                
                foreach (var face in facesSorted)
                {
                    var comment = string.Empty;
                    if (count == 1)
                    {
                        comment = "Happiest :)"; 
                    }
                    if (count == facesSorted.Count)
                    {
                        comment = "Saddest :(";
                    }
                    var faceWithPosition = new FaceWithPosition()
                    {
                        Face = face,
                        Position = count,
                        Comment = comment
                    };
                    facesWithPosition.Add(faceWithPosition);
                    count += 1;
                }

                //add list of faces to view model
                vm.Faces = facesWithPosition;
            }

            return View(vm);
        }

        public IActionResult About()
        {
            ViewData["Message"] = "Your application description page.";

            return View();
        }

        public IActionResult Contact()
        {
            ViewData["Message"] = "Your contact page.";

            return View();
        }

        public IActionResult Error()
        {
            return View();
        }
    }
}
