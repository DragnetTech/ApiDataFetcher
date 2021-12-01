using System;
using CommandLine;
using SigParserApi.Formatters;
using SigParserApi.Verbs;

namespace SigParserApi
{
    class Program
    {
        static int Main(string[] args)
        {
            return Parser.Default.ParseArguments<FetchContactsOptions, FetchCompaniesOptions>(args)
                .MapResult((FetchContactsOptions o) =>
                {
                    IFormatter formatter;
                    if (o.Formatter == "jsonArray")
                    {
                        formatter = new JsonArrayFormatter();
                    }
                    else if (o.Formatter == "jsonLines")
                    {
                        formatter = new JsonLinesFormatter();
                    }
                    else
                    {
                        throw new Exception("Option 'formatter' - provided value is invalid.");
                    }

                    var db = new LocalDB();
                    var fetchContacts = new FetchContacts(o, formatter, db);
                    fetchContacts.Fetch().Wait();
                    
                    return 0;
                }, (FetchCompaniesOptions o) =>
                {
                    Console.WriteLine("Not implemented.");
                    
                    return 0;
                }, (errs) => 1);
        }
    }
}