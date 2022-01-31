using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SigParserApi.Formatters
{
    public class JsonArrayFormatter : IFormatter
    {
        public async Task GenerateFile(string workingDirectory, string outputFile, Encoding encoding)
        {
            string docPath = Directory.GetCurrentDirectory() + workingDirectory;
            string[] inputFilePaths = Directory.GetFiles(docPath, "*.json");

            await using var outputStream = File.Create(outputFile);
            outputStream.Write(encoding.GetBytes("["));

            int i = 0;
            foreach (var inputFilePath in inputFilePaths)
            {
                if (i > 0)
                {
                    outputStream.Write(encoding.GetBytes(","));
                }
                
                i++;
                if (i % 100 == 0)
                {
                    Console.WriteLine($"Appended {i} records to {outputFile}.");
                }

                var fileContents = await File.ReadAllBytesAsync(inputFilePath);
                var convertedBytes = Encoding.Convert(Encoding.UTF8, encoding, fileContents);
                await outputStream.WriteAsync(convertedBytes);
            }
            
            outputStream.Write(encoding.GetBytes("]"));
        }
    }
}