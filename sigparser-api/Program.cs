using System;
using CommandLine;
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
                    var fetchContacts = new FetchContacts(o);
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