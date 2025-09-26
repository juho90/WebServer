namespace TestClient.Commands
{
    public class CommandArgSpec(string name, CommandArgType type, bool required = true)
    {
        public string Name { get; set; } = name;
        public string? Dest { get; set; } = null;
        public CommandArgType Type { get; set; } = type;
        public int? Min { get; set; } = null;
        public int? Max { get; set; } = null;
        public bool Required { get; set; } = required;
    }

    public enum CommandArgType
    {
        String,
        Int,
    }
}
