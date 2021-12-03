using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using CommandLine;
using RestSharp;
using SigParserApi.Formatters;

namespace SigParserApi.Verbs
{
    [Verb("fetch-contacts", HelpText = "Fetches contacts and builds one giant file with all contact details. May run slow on first execution, but with mostly cached results on the next run.")]
    public class FetchContactsOptions
    {
        [Option('o', "output", Required = true, HelpText = "The name of the output file. Make sure to add the file type, likely .json.")]
        public string Output { get; set; } = "contacts.json";
        
        [Option('a',"apikey", Required = false, HelpText = "Optional. SigParser API key.")]
        public string? ApiKey { get; set; }

        [Option('f',"formatter", Required = false, Default = "jsonArray", HelpText = "Optional. Configure the output format. Options: [jsonArray, jsonLines]")]
        public string Formatter { get; set; } = "jsonArray";

        [Option('m', "expand-relationship-metrics", Required = false, Default = false, HelpText = @"Include the relationship_metrics array on each contact in the response.
            This can add a lot of size to the response payload so it is suggested you not include it when you don't need it.")]
        public bool ExpandRelationshipMetrics { get; set; }
        
        [Option('h', "expand-relationship-metrics-history", Required = false, Default = false, HelpText = "Expand the history within the relationship metrics. This may expand the response size of the request considerably.")]
        public bool ExpandRelationshipMetricsHistory { get; set; }
        
        [Option('t', "expand-relationship-metrics-type", Required = false, HelpText = "Which contacts should be in the 'relationship_metrics' field. Options: [INTERNAL, EXTERNAL, ALL]")]
        public string? ExpandRelationshipMetricsType { get; set; }
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
            var fileCount = 0;
            var restClient = new RestClient("https://ipaas.sigparser.com");
            var apiKey = _options.ApiKey ?? Environment.GetEnvironmentVariable("SigParserApiKey");
            restClient.AddDefaultHeader("x-api-key", apiKey);

            while (true)
            {
                var restRequest = new RestRequest("/api/Contacts/List", Method.POST);
                restRequest.AddJsonBody(new
                {
                    take = 100, 
                    orderbyasc = true, 
                    lastmodified_after = state.ContactsLastModified ?? "1950-01-01T01:00:00+00:00",
                    expand_relationship_metrics = _options.ExpandRelationshipMetrics,
                    expand_relationship_metrics_history = _options.ExpandRelationshipMetricsHistory,
                    expand_relationship_metrics_type = _options.ExpandRelationshipMetricsType
                });
            
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

                fileCount += doc.RootElement.GetArrayLength();
                Console.WriteLine($"Fetched {fileCount} contacts.");
                if (doc.RootElement.GetArrayLength() < 100) break;
            }
            
            await _formatter.GenerateFile(workingDirectory: WorkingDirPath, outputFile: _options.Output);
            Console.WriteLine("Finished processing files.");
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