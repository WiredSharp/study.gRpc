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
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Google.Protobuf;
using Greeter.Helloworld;
using Greeter.Routeguide;
using Grpc.Core;

namespace GreeterClient
{
  internal class Program
  {
    public static void Main(string[] args)
    {
      Channel channel = new Channel("127.0.0.1:50051", ChannelCredentials.Insecure);

      //DateTime now = DateTime.Now.AddMilliseconds(200).ToUniversalTime();

      CancellationTokenSource trigger = new CancellationTokenSource();
      CancellationToken cancellationToken = trigger.Token;

      if (!Task.WhenAll(
        TestRecordRouteAsync(channel, 5, 5, trigger.Token), 
        Task.Run(() =>
        {
          Console.WriteLine("press any key to interrupt...");
          Console.ReadKey();
          trigger.Cancel();
        }, cancellationToken)).Wait(TimeSpan.FromSeconds(10)))
      {
        Console.WriteLine("record routes timeout");
      }

      trigger.Cancel();
      trigger.Dispose();

      if (!channel.ShutdownAsync().Wait(TimeSpan.FromSeconds(5)))
      {
        Console.WriteLine("channel shutdown timeout");
      }
      //Console.WriteLine("Press any key to exit...");
      //Console.ReadKey();
    }

    private static void TestRouteGuide(Channel channel, int latitude, DateTime deadline)
    {
      var client = new RouteGuide.RouteGuideClient(channel);
      try
      {
        Feature feature = client.GetFeature(new Point() { Latitude = latitude, Longitude = 34 }, new CallOptions(deadline:deadline));
        Console.WriteLine($"{feature.Name}");
      }
      catch (RpcException e)
      {
        Console.WriteLine($"ex status: {e.Status}");
      }
    }

    private static async Task TestRecordRouteAsync(Channel channel, int deltaLatitude = 5, int deltaLongitude = 5, CancellationToken cancellationToken = default)
    {
      var client = new RouteGuide.RouteGuideClient(channel);
      const int longitude = 34;
      const int latitude = 1;
      //var tasks = new List<Task>(deltaLatitude * deltaLongitude);
      using (AsyncClientStreamingCall<Point, RouteSummary> input = client.RecordRoute())
      {
        for (int lat = latitude; lat < latitude + deltaLatitude; lat++)
        {
          for (int lo = longitude; lo < longitude + deltaLongitude; lo++)
          {
            Console.WriteLine($"queuing point {lat}:{lo}...");
            //tasks.Add(input.RequestStream.WriteAsync(new Point() {Latitude = lat, Longitude = lo}));
            await input.RequestStream.WriteAsync(new Point() {Latitude = lat, Longitude = lo}).ConfigureAwait(false);
          }
        }
        //await Task.WhenAll(tasks).ConfigureAwait(false);
        Console.WriteLine($"all {deltaLongitude*deltaLatitude} points sent");
        await input.RequestStream.CompleteAsync().ConfigureAwait(false);
        Console.WriteLine("request stream closed");
        RouteSummary summary = await input.ResponseAsync.ConfigureAwait(false);
        Console.WriteLine($"Point count: {summary.PointCount}");
      }
    }

    private static async Task TestListFeaturesAsync(Channel channel, int deltaLatitude = 5, int deltaLongitude = 5, CancellationToken cancellationToken = default)
    {
      var client = new RouteGuide.RouteGuideClient(channel);
      const int longitude = 34;
      const int latitude = 1;
      using (var request = client.ListFeatures(new Rectangle()
      {
        Lo = new Point() {Latitude = latitude, Longitude = longitude}
        , Hi = new Point() { Latitude = latitude+deltaLatitude, Longitude = longitude+deltaLongitude }
      }, cancellationToken:cancellationToken))
      {
        try
        {
          bool hasNext = await request.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);
          while (hasNext)
          {
            //Console.WriteLine($"status: {request.GetStatus()}"); status can only be accessed once the call has finished
            Console.WriteLine(request.ResponseStream.Current.Name);
            hasNext = await request.ResponseStream.MoveNext(cancellationToken).ConfigureAwait(false);
          }
        }
        catch (RpcException e)
        {
          Console.WriteLine($"request status: {request.GetStatus()}");
          Console.WriteLine($"ex status: {e.Status}");
        }
      }
    }

    private static async Task TestGetFeatureAsync(Channel channel, int latitude, DateTime deadline)
    {
      var client = new RouteGuide.RouteGuideClient(channel);
      using (var request = client.GetFeatureAsync(new Point() { Latitude = latitude, Longitude = 34 }, new CallOptions(deadline: deadline)))
      {
        try
        {
          Metadata headers = await request.ResponseHeadersAsync.ConfigureAwait(false);
          foreach (Metadata.Entry header in headers)
          {
            Console.WriteLine($"[{header.Key}]:{header.Value}");
          }
          Feature feature = await request.ResponseAsync.ConfigureAwait(false);
          Console.WriteLine($"status: {request.GetStatus()}");
          Console.WriteLine($"feature: {feature.Name}");
        }
        catch (RpcException e)
        {
          Console.WriteLine($"request status: {request.GetStatus()}");
          Console.WriteLine($"ex status: {e.Status}");
        }
      }
    }

    private static void TestHelloWorld(Channel channel)
    {
      var client = new HelloWorld.HelloWorldClient(channel);
      String user = "you";

      var helloRequest = new HelloRequest { Name = user };

      HelloReply reply = client.SayHello(helloRequest);
      Console.WriteLine("Greeting: " + reply.Message);

      reply = client.SayHelloAgain(helloRequest);
      Console.WriteLine("Greeting: " + reply.Message);
    }
  }
}
