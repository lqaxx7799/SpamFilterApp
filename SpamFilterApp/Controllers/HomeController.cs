using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using SpamFilterApp.Models;
using SpamFilterApp.Spam;

namespace SpamFilterApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Train()
        {
            return View();
        }

        public IActionResult Test()
        {
            return View();
        }

        public IActionResult Gmail()
        {
            return View();
        }

        public IActionResult TrainSpam()
        {
            var result = SpamModel.Train();
            return Json(new
            {
                result
            });
        }

        [HttpPost]
        public IActionResult TestSpam([FromBody] string message)
        {
            var result = SpamModel.Predict(message);
            return Json(new
            {
                result
            });
        }

        [HttpPost]
        public IActionResult CheckEmailSpam([FromBody]MailContent[] data)
        {
            List<MailPredictResult> result = new List<MailPredictResult>();
            var groupedBySender = data.GroupBy(m => m.From);
            foreach (var mailsBySender in groupedBySender)
            {
                MailPredictResult spamResult = new MailPredictResult()
                {
                    SenderEmail = mailsBySender.Key.Contains('<') ? mailsBySender.Key.Split('<', '>')[1] : mailsBySender.Key,
                    TotalSentEmail = mailsBySender.Count(),
                    TotalSpam = 0,
                    Spams = new List<FormattedEmail>()
                };
                foreach (var mail in mailsBySender)
                {
                    string mailMessage = "";
                    foreach (var part in mail.Parts)
                    {
                        mailMessage += SpamModel.Base64Decode(part.Data) + " ";
                    }
                    var prediction = SpamModel.Predict(mailMessage);
                    if (prediction.IsSpam)
                    {
                        spamResult.TotalSpam = spamResult.TotalSpam + 1;
                        spamResult.Spams.Add(new FormattedEmail()
                        {
                            Id = mail.Id,
                            Content = mailMessage,
                            Prediction = prediction
                        });
                    }
                }
                result.Add(spamResult);
            }
            return Json(new
            {
                result
            });

        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }

    public class TestClass
    {
        public string Aaa { get; set; }
        public int Bbb { get; set; }
    }
}
