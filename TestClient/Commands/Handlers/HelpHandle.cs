namespace TestClient.Commands.Handlers
{
    public class HelpHandle
    {
        public static async Task Handler(CommandInput input)
        {
            if (!input.ArgDict.TryGetValue("name", out var argValue))
            {
                OnFailed(input.Registry);
                return;
            }
            if (argValue is not string name)
            {
                OnFailed(input.Registry);
                return;
            }
            if (!input.Registry.TryGet(name, out var command))
            {
                OnFailed(input.Registry);
                return;
            }
            Console.WriteLine($"\n{command.Name}: {command.Desc}\n사용법: {command.Usage}\n인수:");
            foreach (var argSpec in command.ArgSpecs)
            {
                Console.WriteLine($" - {argSpec.Name} : {argSpec.Type} {(argSpec.Required ? "(필수)" : "(선택)")}");
            }
            Console.WriteLine();
            await Task.CompletedTask;
        }

        public static void OnFailed(CommandRegistry registry)
        {
            Console.WriteLine("\n사용 가능한 명령:");
            foreach (var item in registry.All())
            {
                Console.WriteLine($" {item.Name,-10} {item.Desc}");
            }
            Console.WriteLine("\n자세한 도움말: help <명령>\n");
        }
    }
}
