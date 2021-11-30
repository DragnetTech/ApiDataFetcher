using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Security.Cryptography;
using CommandLine;
using RestSharp;
using SigParserApi.Formatters;

namespace SigParserApi.Verbs
{
    [Verb("fetch-contacts", HelpText = "Fetches contacts from the sigparser api.")]
    public class FetchContactsOptions
    {   
        [Option("output", Required = true, HelpText = "This is the name of the output file.")]
        public string Output { get; set; }
        
        [Option("apikey", Required = false)]
        public string? ApiKey { get; set; }

        [Option("formatter", Required = false, Default = "jsonArray", HelpText = "Configure the output format. Options: jsonArray, jsonLines")]
        public string Formatter { get; set; } = "jsonArray";
    }

    public class FetchContacts
    {
        private FetchContactsOptions _options;
        private IFormatter _formatter;
        private LocalDB _db;
        private const string WorkingDirPath = "/sigparser-api-files/fetch-contacts";
        public FetchContacts(FetchContactsOptions options, IFormatter formatter, LocalDB db)
        {
            _options = options;
            _formatter = formatter;
            _db = db;
        }

        public async Task Fetch()
        {
            var state = _db.LoadState();
            
            var restClient = new RestClient("https://ipaas.sigparser.com");
            var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("SigParserApiKey");
            restClient.AddDefaultHeader("x-api-key", apiKey);

            while (true)
            {
                var restRequest = new RestRequest("/api/Contacts/List", Method.POST);
                restRequest.AddJsonBody(new { take = 100, orderbyasc = true, lastmodified_after = state.ContactsLastModified ?? "2000-11-20T17:32:59+00:00" });
            
                var response = await restClient.ExecuteAsync(restRequest);
                if (!response.IsSuccessful) throw new Exception($"Error: {response.StatusCode} {response.Content}");
                
                var doc = JsonDocument.Parse(response.Content);
                Console.WriteLine($"fetched {doc.RootElement.GetArrayLength()} contacts {state.ContactsLastModified}");
                
                var lastModified = "";
                foreach (var element in doc.RootElement.EnumerateArray())
                {
                    lastModified = element.GetProperty("lastmodified").ToString();
                    var email = element.GetProperty("emailaddress").ToString();
                    var hash = GetHash(email);

                    WriteToFile(element.ToString(), hash + ".json");
                }
            
                state.ContactsLastModified = lastModified;
                _db.SaveState(state);

                if (doc.RootElement.GetArrayLength() < 100) break;
            }
            await _formatter.GenerateFile(workingDirectory: WorkingDirPath, outputFile: _options.Output);
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
            string docPath = Directory.GetCurrentDirectory() + WorkingDirPath;
            Directory.CreateDirectory(docPath);
            
            using (StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, fileName)))
            {
                await outputFile.WriteAsync(contents);
            }
        }
    }
}