using TestClient.Commands;
using TestClient.Commands.Handlers;

namespace TestClient
{
    public class CommandRegister
    {
        public static void Register(CommandRegistry registry)
        {
            registry.Add(new("help", "명령 목록 또는 특정 명령의 세부 도움말 표시", "help [명령]")
            {
                ArgSpecs = [new("name", CommandArgType.String, required: false)],
                Handler = HelpHandle.Handler
            });
            registry.Add(new("enqueue", "서버에 다수의 대기열 생성", "enqueue [count] [each]")
            {
                ArgSpecs = [
                    new("count", CommandArgType.Int, required: true)
                    {
                        Min = 10,
                        Max = 100
                    },
                    new("each", CommandArgType.Int, required: true)
                    {
                        Min = 25,
                        Max = 50
                    }
                ],
                Handler = EnqueueHandle.Handler
            });
        }
    }
}
