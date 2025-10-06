using Grpc.Core;
using MyProtos;

namespace GameServer.GrpcServices
{
    public class GreeterGrpcService() : Greeter.GreeterBase
    {
        public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
        {
            return Task.FromResult(new HelloReply
            {
                Message = "Hello " + request.Name
            });
        }
    }
}
