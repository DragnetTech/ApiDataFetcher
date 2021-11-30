using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;
using RestSharp;
using SigParserApi.Formatters;

namespace SigParserApi.Verbs
{
    [Verb("fetch-contacts", HelpText = "Fetches contacts from the sigparser api.")]
    public class FetchContactsOptions
    {
        [Option("output", Required = true, HelpText = "The name of the output file. Make sure to add the file type, likely .json.")]
        public string Output { get; set; } = "contacts.json";
        
        [Option("apikey", Required = false, HelpText = "Optional: SigParser API key.")]
        public string? ApiKey { get; set; }

        [Option("formatter", Required = false, Default = "jsonArray", HelpText = "Optional: Configure the output format. Options: jsonArray, jsonLines. Default: jsonArray.")]
        public string Formatter { get; set; } = "jsonArray";
    }

    public class FetchContacts
    {
        private readonly FetchContactsOptions _options;
        private readonly IFormatter _formatter;
        private readonly LocalDB _db;
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
                restRequest.AddJsonBody(new { take = 100, orderbyasc = true, lastmodified_after = state.ContactsLastModified });
            
                var response = await restClient.ExecuteAsync(restRequest);
                if (!response.IsSuccessful) throw new Exception($"Error: {response.StatusCode} {response.Content}");

                var doc = JsonDocument.Parse(response.Content);
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
    
                Console.WriteLine($"fetched {doc.RootElement.GetArrayLength()} contacts {state.ContactsLastModified}");
                if (doc.RootElement.GetArrayLength() < 100) break;
            }
            await _formatter.GenerateFile(workingDirectory: WorkingDirPath, outputFile: _options.Output);
        }
        
        private static string GetHash(string text)
        {
            if (String.IsNullOrEmpty(text)) return String.Empty;

            using var sha = new System.Security.Cryptography.SHA256Managed();
            byte[] textData = System.Text.Encoding.UTF8.GetBytes(text);
            byte[] hash = sha.ComputeHash(textData);
            return BitConverter.ToString(hash).Replace("-", String.Empty);
        }

        private async void WriteToFile(string contents, string fileName)
        {
            string docPath = Directory.GetCurrentDirectory() + WorkingDirPath;
            Directory.CreateDirectory(docPath);

            await using StreamWriter outputFile = new StreamWriter(Path.Combine(docPath, fileName));
            await outputFile.WriteAsync(contents);
        }
    }
}