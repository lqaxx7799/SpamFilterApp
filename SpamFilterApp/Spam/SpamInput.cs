using Microsoft.ML.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SpamFilterApp.Spam
{
    /// <summary>
    /// The SpamInput class contains one single message which may be spam or ham.
    /// </summary>
    public class SpamInput
    {
        [LoadColumn(0)] public string RawLabel { get; set; }
        [LoadColumn(1)] public string Message { get; set; }
    }
}
