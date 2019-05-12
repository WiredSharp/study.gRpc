using System;
using System.Threading.Tasks;
using Greeter.Routeguide;
using Grpc.Core;

namespace GreeterServer
{
  internal class RouteGuideService : RouteGuide.RouteGuideBase
  {
    private static readonly Feature DefaultFeature = new Feature();
    private static readonly RouteSummary DefaultRouteSummary = new RouteSummary();

    public event EventHandler<EventArgs<string>> OnReceived;
    

    public override Task<Feature> GetFeature(Point request, ServerCallContext context)
    {
      Notify($"get feature {request?.Latitude} {request?.Longitude}");
      context.Status = new Status((StatusCode)(request?.Latitude ?? 0), $"sending status code {request?.Latitude}");
      //if (context.Status.StatusCode == StatusCode.OK)
      return Task.FromResult(new Feature() {Name = $"feature {context.Status}", Location = request});
      //return Task.FromResult((Feature)null);
    }

    public override async Task ListFeatures(Rectangle request, IServerStreamWriter<Feature> responseStream, ServerCallContext context)
    {
      Notify($"list features {request?.Lo?.Latitude}:{request?.Lo?.Longitude} / {request?.Hi?.Latitude}:{request?.Hi?.Longitude}");
      if (request?.Lo != null && request?.Hi != null)
      {
        for (int lat = request.Lo.Latitude; lat < request.Hi.Latitude; lat++)
        {
          for (int lo = request.Lo.Longitude; lo < request.Hi.Longitude; lo++)
          {
            if (context.CancellationToken.IsCancellationRequested)
            {
              context.Status = Status.DefaultCancelled;
              return;
            }
            await responseStream.WriteAsync(new Feature()
              {
                Name = $"feature {lat}:{lo}"
                , Location = new Point() { Latitude = lat, Longitude = lo}
              }).ConfigureAwait(false);
          }
        }
      }
    }

    public override async Task<RouteSummary> RecordRoute(IAsyncStreamReader<Point> requestStream, ServerCallContext context)
    {
      Notify("record route...");
      DateTime start = DateTime.Now;
      var summary = new RouteSummary();
      bool hasNext = await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false);
      while (hasNext)
      {
        if (context.CancellationToken.IsCancellationRequested)
        {
          context.Status = Status.DefaultCancelled;
          return DefaultRouteSummary;
        }
        var point = requestStream.Current;
        Notify($"receiving one new point {point.Latitude}:{point.Longitude}");
        summary.PointCount++;
        hasNext = await requestStream.MoveNext(context.CancellationToken).ConfigureAwait(false);
      }
      summary.ElapsedTime = (int)(DateTime.Now - start).TotalMilliseconds;
      Notify("...route recorded");
      return summary;
    }
    public override Task RouteChat(IAsyncStreamReader<RouteNote> requestStream, IServerStreamWriter<RouteNote> responseStream, ServerCallContext context)
    {
      Notify("start route chat...");
      context.Status = new Status(StatusCode.Unimplemented, "not implemented");
      return Task.CompletedTask;
    }

    private void Notify(string message)
    {
      OnReceived?.Invoke(this, new EventArgs<string>(message));
    }
  }

  public class EventArgs<TPayload>
  {
    public TPayload Payload;

    public EventArgs(TPayload payload)
    {
      Payload = payload;
    }
  }
}
