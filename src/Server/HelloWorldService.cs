using System.Collections.Generic;
using System.Threading.Tasks;
using Greeter.Helloworld;
using Grpc.Core;

namespace GreeterServer
{
  internal class HelloWorldService : HelloWorld.HelloWorldBase
  {
    public IDictionary<string, int> Users { get; } = new Dictionary<string, int>();

    // Server side handler of the SayHello RPC
    public override Task<HelloReply> SayHello(HelloRequest request, ServerCallContext context)
    {
      Users.TryGetValue(request.Name, out var visits);
      Users[request.Name] = visits + 1;
      string message = visits > 0 ? $"Hello {request.Name} for the {visits + 1} times..." : $"Hello {request.Name}";
      return Task.FromResult(new HelloReply { Message = message });
    }

    public override Task<HelloReply> SayHelloAgain(HelloRequest request, ServerCallContext context)
    {
      return Task.FromResult(new HelloReply { Message = $"Hello {request.Name} again..." });
    }
  }
}