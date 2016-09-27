using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using HowHappy.ViewModels;
using HowHappy.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.AspNetCore.Http;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Encodings.Web;

namespace HowHappyCore.Controllers
{
    public class HomeController : Controller
    {
        //_apiKey: Replace this with your own Project Oxford Emotion API key, please do not use my key. I include it here so you can get up and running quickly but you can get your own key for free at https://www.projectoxford.ai/emotion 
        public const string _emotionApiKey = "1dd1f4e23a5743139399788aa30a7153";

        //_apiUrl: The base URL for the API. Find out what this is for other APIs via the API documentation
        public const string _emotionApiUrl = "https://api.projectoxford.ai/emotion/v1.0/recognize";

        public const string _luisApiUrl = "https://api.projectoxford.ai/luis/v1/application?";

        public const string _luisApiAppId = "f91bf390-537f-4e76-809d-eb34c2ed1ac4";

        public const string _luisApiKey = "d004b0b064694dd1bec537e3629863fb";

        public IActionResult Index()
        {
            //clear any existing session data
            SetSessionData("emotiondata", string.Empty);

            //create view model
            var emotion = "happiness";
            var vm = new ResultViewModel()
            {
                Faces = null,
                Emotion = emotion,
                Emotions = GetEmotionSelectList(),
                ThemeColour = GetThemeColour(emotion),
                FAEmotionClass = GetEmojiClass(emotion)
            };

            return View(vm);
        }



        [HttpPost]
        public async Task<IActionResult> Result()
        {
            //get form data
            var emotion = Request.Form.ContainsKey("emotion") ?
                Request.Form["emotion"].ToString() :
                "happiness";
            var luisQuery = Request.Form.ContainsKey("luisquery") ?
                Request.Form["luisquery"].ToString() :
                string.Empty;

            //get emotion from luis if we have a luis query
            if (!string.IsNullOrEmpty(luisQuery))
            {
                emotion = await GetEmotionFromLUIS(luisQuery);
            }

            //get faces list if session data is empty or there is a file in the form
            if (string.IsNullOrEmpty(ReadSessionData("emotiondata")))
            {
                //get file from form data
                var file = Request.Form.Files[0];

                //get emotion data from api and store it in session
                var emotionDataString = await GetEmotionData(file);
                SetSessionData("emotiondata", emotionDataString);
            }

            //get faces list
            var emotionData = ReadSessionData("emotiondata");
            var faces = GetFaces(emotionData);

            //create view model
            var vm = new ResultViewModel()
            {
                Faces = GetSortedFacesList(faces, emotion),
                Emotion = emotion,
                Emotions = GetEmotionSelectList(),
                ThemeColour = GetThemeColour(emotion),
                FAEmotionClass = GetEmojiClass(emotion)
            };

            return Json(vm);
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
            if (bytes == null)
            {
                return string.Empty;
            }
            else
            {
                char[] chars = new char[bytes.Length / sizeof(char)];
                Buffer.BlockCopy(bytes, 0, chars, 0, bytes.Length);
                return new string(chars);
            }

        }

        private void SetSessionData(string key, string value)
        {
            byte[] valueAsBytes = new byte[value.Length * sizeof(char)];
            System.Buffer.BlockCopy(value.ToCharArray(), 0, valueAsBytes, 0, valueAsBytes.Length);
            HttpContext.Session.Set(key, valueAsBytes);
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
                httpClient.BaseAddress = new Uri(_emotionApiUrl);
                httpClient.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", _emotionApiKey);
                var content = new StreamContent(file.OpenReadStream());
                content.Headers.ContentType = new MediaTypeWithQualityHeaderValue("application/octet-stream");

                //make request
                var responseMessage = await httpClient.PostAsync(_emotionApiUrl, content);

                //read response as a json string
                responseString = await responseMessage.Content.ReadAsStringAsync();
            }

            return responseString;
        }

        private async Task<string> GetEmotionFromLUIS(string utterance)
        {
            var responseString = string.Empty;

            //call emotion api
            using (var httpClient = new HttpClient())
            {
                //setup HttpClient with content
                httpClient.BaseAddress = new Uri(_luisApiUrl);

                //https://api.projectoxford.ai/luis/v1/application?id=f91bf390-537f-4e76-809d-eb34c2ed1ac4&subscription-key=d004b0b064694dd1bec537e3629863fb&q=who%20is%20the%20happiest
                var queryUrl = _luisApiUrl + "id=" + _luisApiAppId + "&subscription-key=" + _luisApiKey + "&q=" + UrlEncoder.Default.Encode(utterance);

                //make request
                var responseMessage = await httpClient.GetAsync(queryUrl);

                //read response as a json string
                responseString = await responseMessage.Content.ReadAsStringAsync();


                //deserialise json to luis response
                var luisResult = JsonConvert.DeserializeObject<LuisResult>(responseString);

                //get the emotion intent
                if (luisResult.entities.Count > 0)
                {
                    var entity = luisResult.entities.FirstOrDefault();
                    //strip emotion:: from the start of type
                    var emotion = entity.type.Substring(9);
                    responseString = emotion;
                }
                else
                {
                    responseString = "happiness";
                }

            }

            return responseString;
        }

        public IActionResult Error() => View();

    }
}
