using System;
using CommandLine;
using SigParserApi.Formatters;
using SigParserApi.Verbs;

namespace SigParserApi
{
    class Program
    {
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<FetchContactsOptions>(args)
                .WithParsed<FetchContactsOptions>(o =>
                {
                    IFormatter formatter;
                    if (o.Formatter == "jsonArray")
                    {
                        formatter = new JsonArrayFormatter();
                    }
                    else if(o.Formatter == "jsonLines")
                    {
                        formatter = new JsonLinesFormatter();
                    }
                    else
                    {
                        throw new Exception("Option 'formatter' - provided value is invalid.");
                    }

                    var db = new LocalDB();
                    var fetchContacts = new FetchContacts(o,formatter, db);
                    fetchContacts.Fetch().Wait();
                }).WithNotParsed(errors =>
                {
                    foreach (var error in errors)
                    {
                        Console.WriteLine(error.ToString());
                    }
                });
        }
    }
}