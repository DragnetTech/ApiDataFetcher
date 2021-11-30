using System.IO;
using System.Text.Json;

namespace SigParserApi
{
    public class LocalDB
    {
        public const string StateFile = "state.json";

        public State LoadState()
        {
            if (File.Exists(StateFile))
            {
                var json = File.ReadAllText(StateFile);
                State state = JsonSerializer.Deserialize<State>(json);
                return state;
            }

            return new State();
        }

        public void SaveState(State state)
        {
            var json = JsonSerializer.Serialize(state);
            File.WriteAllText(StateFile, json);
        }
    }
}