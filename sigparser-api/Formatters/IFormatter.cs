using System.Text;
using System.Threading.Tasks;

namespace SigParserApi.Formatters
{
    public interface IFormatter
    {
        Task GenerateFile(string workingDirectory, string outputFile, Encoding encoding);
    }
}