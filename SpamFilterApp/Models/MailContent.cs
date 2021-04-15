using SpamFilterApp.Spam;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpamFilterApp.Models
{
    public class MailContent
    {
        public string Id { get; set; }
        public string From { get; set; }
        public Part[] Parts { get; set; }
    }

    public class Part
    {
        public string Data { get; set; }
    }

    public class FormattedEmail
    {
        public string Id { get; set; }
        public string Content { get; set; }
        public SpamPrediction Prediction { get; set; }
    }

    public class MailPredictResult
    {
        public string SenderEmail { get; set; }
        public int TotalSentEmail { get; set; }
        public int TotalSpam { get; set; }
        public List<FormattedEmail> Spams { get; set; }
    }
}
