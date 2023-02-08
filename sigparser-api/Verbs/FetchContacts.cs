using System;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Text;
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

        [Option("codepage", Required = false, HelpText = "The encoding code page of the final output file. This is always a number. If not set then UTF-8 is the default. This must be a number. This was introduced to help with importing into SQL Server using OPENROWSET. Example: 1252")]
        public int? CodePageEncoding { get; set; }
    }

    public class FetchContacts
    {
        private readonly FetchContactsOptions _options;
        private readonly IFormatter _formatter;
        private readonly LocalDB _db;
        private const string WorkingDirPath = "/sigparser-api-files/fetch-contactsV2";
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
                    lastmodified_after = state.ContactsLastModifiedV2 ?? "1950-01-01T01:00:00+00:00",
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
                    var internal_id = element.GetProperty("internal_id").ToString();

                    WriteToFile(element.ToString(), internal_id + ".json");
                }
            
                state.ContactsLastModifiedV2 = lastModified;
                _db.SaveState(state);

                fileCount += doc.RootElement.GetArrayLength();
                Console.WriteLine($"Fetched {fileCount} contacts.");
                if (doc.RootElement.GetArrayLength() < 100) break;
            }
            
            Encoding encoding = Encoding.UTF8;
            if (_options.CodePageEncoding != null)
            {
                encoding = Encoding.GetEncoding(_options.CodePageEncoding.Value);
            }
            
            await _formatter.GenerateFile(workingDirectory: WorkingDirPath, outputFile: _options.Output, encoding: encoding);
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