using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using CommandLine;
using RestSharp;

namespace SigParserApi.Verbs
{
    [Verb("fetch-contacts", HelpText = "Fetches contacts from the sigparser api.")]
    public class FetchContactsOptions
    {   
        [Option("output")]
        public string Output { get; set; }
        
        [Option("apikey", Required = false)]
        public string? ApiKey { get; set; }
    }

    public class FetchContacts
    {
        private FetchContactsOptions _options;
        public FetchContacts(FetchContactsOptions options)
        {
            _options = options;
        }

        public async Task Fetch()
        {
            var restClient = new RestClient("https://ipaas.sigparser.com");
            var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("SigParserApiKey");
            restClient.AddDefaultHeader("x-api-key", apiKey);
            var restRequest = new RestRequest("/api/Contacts/List", Method.POST);
            restRequest.AddJsonBody(new { take = 100, orderbyasc = true, lastmodified_after = "2000-11-20T17:32:59+00:00" });
            
            var response = await restClient.ExecuteAsync(restRequest);
            var doc = JsonDocument.Parse(response.Content);
       
            var lastModified = "";
            foreach (var element in doc.RootElement.EnumerateArray())
            {
                lastModified = element.GetProperty("lastmodified").ToString();
                var email = element.GetProperty("emailaddress").ToString();
                var hash = GetHash(email);    

                WriteToFile(element.ToString(), hash + ".json");
            }

            WriteToFile(lastModified,"config.txt");
            JoinFiles();
        }
        
        private static string GetHash(string text)
        {
            if (String.IsNullOrEmpty(text)) return String.Empty;

            using (var sha = new System.Security.Cryptography.SHA256Managed())
            {
                byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
                byte[] hash = sha.ComputeHash(textData);
                return BitConverter.ToString(hash).Replace("-", String.Empty);
            }
        }

        private async void WriteToFile(string contents, string fileName)
        {
            string docPath = Directory.GetCurrentDirectory() + "/temp";
            Directory.CreateDirectory(docPath);
            
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, fileName)))
            {
                await outputFile.WriteAsync(contents);
            }
        }

        private async void JoinFiles()
        {
            string docPath = Directory.GetCurrentDirectory() + "/temp";
            string[] inputFilePaths = Directory.GetFiles(docPath, "*.json");
            string outputFile = _options.Output;
            
            using (var outputStream = File.Create(outputFile))
            {
                foreach (var inputFilePath in inputFilePaths)
                {
                    using (var inputStream = File.OpenRead(inputFilePath))
                    {
                        inputStream.CopyTo(outputStream);
                    }
                    Console.WriteLine("The file {0} has been processed.", inputFilePath);
                }
            }
        }
    }
}