namespace TestClient.Commands
{
    public class CommandRegistry
    {
        private readonly Dictionary<string, Command> map = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string name, out Command cmd)
        {
            return map.TryGetValue(name, out cmd!);
        }

        public void Add(Command cmd)
        {
            map[cmd.Name] = cmd;
        }

        public IEnumerable<Command> All()
        {
            return map.Values.OrderBy(cmd => cmd.Name);
        }
    }
}
