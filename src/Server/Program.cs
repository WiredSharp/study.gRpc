// Copyright 2015 gRPC authors.
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System;
using Greeter.Helloworld;
using Greeter.Routeguide;
using Grpc.Core;

namespace GreeterServer
{
  internal class Program
  {
    private const int Port = 50051;

    public static void Main(string[] args)
    {
      var helloWorldService = new HelloWorldService();
      var routeGuideService = new RouteGuideService();
      routeGuideService.OnReceived += OnReceived;
      Server server = new Server
      {
        Services = { HelloWorld.BindService(helloWorldService), RouteGuide.BindService(routeGuideService) },
        Ports = { new ServerPort("localhost", Port, ServerCredentials.Insecure) }
      };
      server.Start();

      Console.WriteLine("Greeter server listening v1.0.2 on port " + Port);
      Console.WriteLine("Press any key to stop the server...");
      Console.ReadKey();

      server.ShutdownAsync().Wait();
    }

    private static void OnReceived(object sender, EventArgs<string> e)
    {
      Console.WriteLine($">> {e.Payload}");
    }
  }
}
