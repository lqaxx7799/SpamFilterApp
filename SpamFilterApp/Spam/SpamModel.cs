using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.ML;
using Microsoft.ML.Data;
using Microsoft.ML.Transforms;

namespace SpamFilterApp.Spam
{
    /// <summary>
    /// The main program class.
    /// </summary>
    public class SpamModel
    {
        public static PredictionEngine<SpamInput, SpamPrediction> SpamPredictionEngine { get; set; }
        public static Dictionary<string, string> Train()
        {
            string dataPath = Path.Combine(Environment.CurrentDirectory, "Spam\\spam.tsv");
            string modelPath = Path.Combine(Environment.CurrentDirectory, "Spam\\model.zip");

            // set up a machine learning context
            var context = new MLContext();

            // load the spam dataset in memory
            var data = context.Data.LoadFromTextFile<SpamInput>(
                path: dataPath,
                hasHeader: true,
                separatorChar: '\t');

            // use 80% for training and 20% for testing
            var partitions = context.Data.TrainTestSplit(
                data,
                testFraction: 0.2);

            // set up a training pipeline
            // step 1: transform the 'spam' and 'ham' values to true and false
            var pipeline = context.Transforms.CustomMapping(
                new MyLambda().GetMapping(),
                contractName: "MyLambda")

                // step 2: featureize the input text
                .Append(context.Transforms.Text.FeaturizeText(
                    outputColumnName: "Features",
                    inputColumnName: nameof(SpamInput.Message)))

                // step 3: use a stochastic dual coordinate ascent learner
                .Append(context.BinaryClassification.Trainers.SdcaLogisticRegression());

            // test the full data set by performing k-fold cross validation
            Console.WriteLine("Performing cross validation...");
            var cvResults = context.BinaryClassification.CrossValidate(
                data: partitions.TrainSet,
                estimator: pipeline,
                numberOfFolds: 5);

            // report the results
            foreach (var r in cvResults)
                Console.WriteLine($"  Fold: {r.Fold}, AUC: {r.Metrics.AreaUnderRocCurve}");
            Console.WriteLine($"   Average AUC: {cvResults.Average(r => r.Metrics.AreaUnderRocCurve)}");
            Console.WriteLine();

            // train the model on the training set
            Console.WriteLine("Training the model...");
            var model = pipeline.Fit(partitions.TrainSet);

            context.ComponentCatalog.RegisterAssembly(typeof(MyLambda).Assembly);

            //save model
            context.Model.Save(model, partitions.TrainSet.Schema, modelPath);

            // evaluate the model on the test set
            Console.WriteLine("Evaluating the model...");
            var predictions = model.Transform(partitions.TestSet);
            var metrics = context.BinaryClassification.Evaluate(
                data: predictions,
                labelColumnName: "Label",
                scoreColumnName: "Score");

            // report the results
            Console.WriteLine($"  Accuracy:          {metrics.Accuracy:P2}");
            Console.WriteLine($"  Auc:               {metrics.AreaUnderRocCurve:P2}");
            Console.WriteLine($"  Auprc:             {metrics.AreaUnderPrecisionRecallCurve:P2}");
            Console.WriteLine($"  F1Score:           {metrics.F1Score:P2}");
            Console.WriteLine($"  LogLoss:           {metrics.LogLoss:0.##}");
            Console.WriteLine($"  LogLossReduction:  {metrics.LogLossReduction:0.##}");
            Console.WriteLine($"  PositivePrecision: {metrics.PositivePrecision:0.##}");
            Console.WriteLine($"  PositiveRecall:    {metrics.PositiveRecall:0.##}");
            Console.WriteLine($"  NegativePrecision: {metrics.NegativePrecision:0.##}");
            Console.WriteLine($"  NegativeRecall:    {metrics.NegativeRecall:0.##}");
            Console.WriteLine();

            var result = new Dictionary<string, string>();
            result.Add("Accuracy", metrics.Accuracy.ToString());
            result.Add("Auc", metrics.AreaUnderRocCurve.ToString());
            result.Add("Auprc", metrics.AreaUnderPrecisionRecallCurve.ToString());
            result.Add("F1Score", metrics.F1Score.ToString());
            result.Add("LogLoss", metrics.LogLoss.ToString());
            result.Add("LogLossReduction", metrics.LogLossReduction.ToString());
            result.Add("PositivePrecision", metrics.PositivePrecision.ToString());
            result.Add("PositiveRecall", metrics.PositiveRecall.ToString());
            result.Add("NegativePrecision", metrics.NegativePrecision.ToString());
            result.Add("NegativeRecall", metrics.NegativeRecall.ToString());

            SpamPredictionEngine = context.Model.CreatePredictionEngine<SpamInput, SpamPrediction>(model);

            Console.WriteLine("Predicting spam probabilities for a sample messages...");
            var predictionEngine = context.Model.CreatePredictionEngine<SpamInput, SpamPrediction>(model);

            // create sample messages
            var messages = new SpamInput[] {
                new SpamInput() { Message = "Hi, wanna grab lunch together today?" },
                new SpamInput() { Message = "Win a Nokia, PSP, or €25 every week. Txt YEAHIWANNA now to join" },
                new SpamInput() { Message = "Home in 30 mins. Need anything from store?" },
                new SpamInput() { Message = "CONGRATS U WON LOTERY CLAIM UR 1 MILIONN DOLARS PRIZE" },
            };

            // make the prediction
            var myPredictions = from m in messages
                                select (Message: m.Message, Prediction: predictionEngine.Predict(m));

            // show the results
            foreach (var p in myPredictions)
                Console.WriteLine($"  [{p.Prediction.Probability:P2}] {p.Message}");

            return result;
        }

        public static void LoadTrainedModel()
        {
            string modelPath = Path.Combine(Environment.CurrentDirectory, "Spam\\model.zip");
            if (File.Exists(modelPath))
            {
                var context = new MLContext();
                var loadedModel = context.Model.Load(modelPath, out var inputScheme);
                SpamPredictionEngine = context.Model.CreatePredictionEngine<SpamInput, SpamPrediction>(loadedModel);
            }
            else
            {
                Train();
            }
        }

        public static SpamPrediction Predict(string message)
        {
            var spamInput = new SpamInput() { Message = message };

            return SpamPredictionEngine.Predict(spamInput);
        }

        public static string Base64Decode(string base64EncodedData)
        {
            if(base64EncodedData == null)
            {
                return "";
            }
            string s = base64EncodedData;
            s = s.Replace('-', '+'); // 62nd char of encoding
            s = s.Replace('_', '/'); // 63rd char of encoding
            switch (s.Length % 4) // Pad with trailing '='s
            {
                case 0: break; // No pad chars in this case
                case 2: s += "=="; break; // Two pad chars
                case 3: s += "="; break; // One pad char
                default:
                    throw new System.Exception(
             "Illegal base64url string!");
            }
            //base64EncodedData = base64EncodedData.PadRight(base64EncodedData.Length + (4 - base64EncodedData.Length % 4) % 4, '=');
            var base64EncodedBytes = System.Convert.FromBase64String(s);
            return System.Text.Encoding.UTF8.GetString(base64EncodedBytes);
        }

        [CustomMappingFactoryAttribute("MyLambda")]
        private class MyLambda : CustomMappingFactory<FromLabel, ToLabel>
        {
            // We define the custom mapping between input and output rows that will
            // be applied by the transformation.
            public static void CustomAction(FromLabel input, ToLabel output) => output.Label = input.RawLabel == "spam" ? true : false;

            public override Action<FromLabel, ToLabel> GetMapping()
                => CustomAction;
        }
    }


    //context.Transforms.CustomMapping<FromLabel, ToLabel>(
    //            mapAction: (input, output) => { output.Label = input.RawLabel == "spam" ? true : false; },
    //            contractName: "MyLambda")


}