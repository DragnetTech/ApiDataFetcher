using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace SigParserApi.Formatters
{
    public class JsonLinesFormatter : IFormatter
    {
        public async Task GenerateFile(string workingDirectory, string outputFile, Encoding encoding)
        {
            string docPath = Directory.GetCurrentDirectory() + workingDirectory;
            string[] inputFilePaths = Directory.GetFiles(docPath, "*.json");
            await using var outputStream = File.Create(outputFile);

            foreach (var inputFilePath in inputFilePaths)
            {
                await using var inputStream = File.OpenRead(inputFilePath);
                await inputStream.CopyToAsync(outputStream);

                outputStream.Write(Encoding.UTF8.GetBytes("\n"));
            }
        }
    }
}