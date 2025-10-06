namespace TestClient.Commands
{
    public class Command(string name, string desc, string usage)
    {
        public string Name { get; set; } = name;
        public string Desc { get; set; } = desc;
        public string Usage { get; set; } = usage;
        public CommandArgSpec[] ArgSpecs { get; set; } = [];
        public Func<CommandInput, Task> Handler { get; set; } = _ => Task.CompletedTask;

        public CommandValidate ValidateAndBind(string[] args)
        {
            var argDict = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            int argRequiredCount = ArgSpecs.Count(a => a.Required);
            if (args.Length < argRequiredCount || args.Length > ArgSpecs.Length)
            {
                return new CommandValidate
                {
                    Ok = false,
                    ArgDict = null,
                    Error = $"인수 개수 불일치 (필수 {argRequiredCount}, 최대 {ArgSpecs.Length})"
                };
            }
            for (var index = 0; index < ArgSpecs.Length; index++)
            {
                var argSpec = ArgSpecs[index];
                var argValue = index < args.Length ? args[index] : null;
                if (argValue is null)
                {
                    if (argSpec.Required)
                    {
                        return new CommandValidate
                        {
                            Ok = false,
                            ArgDict = null,
                            Error = $"'{argSpec.Name}' 인수 필요"
                        };
                    }
                    continue;
                }
                switch (argSpec.Type)
                {
                    case CommandArgType.Int:
                        if (!int.TryParse(argValue, out var intValue))
                        {
                            return new CommandValidate
                            {
                                Ok = false,
                                ArgDict = null,
                                Error = $"{argSpec.Name} 정수 필요"
                            };
                        }
                        if (argSpec.Min.HasValue && intValue < argSpec.Min.Value)
                        {
                            return new CommandValidate
                            {
                                Ok = false,
                                ArgDict = null,
                                Error = $"{argSpec.Name}>={argSpec.Min}"
                            };
                        }
                        if (argSpec.Max.HasValue && intValue > argSpec.Max.Value)
                        {
                            return new CommandValidate
                            {
                                Ok = false,
                                ArgDict = null,
                                Error = $"{argSpec.Name}<={argSpec.Max}"
                            };
                        }
                        argDict[argSpec.Name] = intValue;
                        break;
                    case CommandArgType.String:
                        argDict[argSpec.Name] = argValue;
                        break;
                }
            }
            return new CommandValidate
            {
                Ok = true,
                ArgDict = argDict,
                Error = null
            };
        }
    }

    public class CommandInput(CommandRegistry registry
        , Dictionary<string, object> argDict
        , string httpUri
        , string grpcUri
        , CancellationToken ct)
    {
        public CommandRegistry Registry { get; set; } = registry;
        public Dictionary<string, object> ArgDict { get; set; } = argDict;
        public string HttpURI { get; set; } = httpUri;
        public string GrpcURI { get; set; } = grpcUri;
        public CancellationToken CancellationToken { get; set; } = ct;
    }

    public class CommandValidate
    {
        public bool Ok { get; set; } = false;
        public Dictionary<string, object>? ArgDict { get; set; } = null;
        public string? Error { get; set; } = null;
    }
}
