using System.Text;

namespace TestClient.Commands
{
    public class CommandArgParser
    {
        public static string[] Split(string input)
        {
            var commands = new List<string>();
            var stringBuilder = new StringBuilder();
            var inQuote = false;
            for (var index = 0; index < input.Length; index++)
            {
                var ch = input[index];
                if (ch == '"')
                {
                    inQuote = !inQuote;
                    continue;
                }
                if (char.IsWhiteSpace(ch) && !inQuote)
                {
                    if (stringBuilder.Length > 0)
                    {
                        commands.Add(stringBuilder.ToString());
                        stringBuilder.Clear();
                    }
                }
                else stringBuilder.Append(ch);
            }
            if (stringBuilder.Length > 0)
            {
                commands.Add(stringBuilder.ToString());
            }
            if (commands.Count <= 0)
            {
                commands.Add("");
            }
            return [.. commands];
        }
    }
}
