using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using EventStore.Client;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Orleans;
using Orleans.EventSourcing;

namespace OrleansEventStore
{
    public interface IShipment : IGrainWithGuidKey
    {
        Task Pickup();
        Task Deliver();
        Task<TransitStatus> GetStatus();
        Task ForceDeactivateForDemo();
    }

    public enum TransitStatus
    {
        AwaitingPickup,
        InTransit,
        Delivered
    }

    public class Shipment : JournaledGrain<ShipmentState>, IShipment
    {
        private readonly ILogger<Shipment> _logger;
        private readonly EventStoreClient _esClient;
        private string _stream;

        public Shipment(ILogger<Shipment> logger)
        {
            _logger = logger;

            var settings = new EventStoreClientSettings
            {
                ConnectivitySettings =
                {
                    Address = new Uri("http://localhost:2113")
                }
            };

            // For demo, assume we're running EventStoreDB locally
            _esClient = new EventStoreClient(settings);
        }

        public override async Task OnActivateAsync()
        {
            _logger.LogDebug("OnActivateAsync");

            _stream = $"Shipment-{this.GetPrimaryKey()}";

            var stream = _esClient.ReadStreamAsync(Direction.Forwards, _stream, StreamPosition.Start);

            if (await stream.ReadState == ReadState.StreamNotFound)
            {
                await ConfirmEvents();
            }

            await foreach (var resolvedEvent in stream)
            {
                object eventObj;
                var json = Encoding.UTF8.GetString(resolvedEvent.Event.Data.Span);
                switch (resolvedEvent.Event.EventType)
                {
                    case "PickedUp":
                        eventObj = JsonConvert.DeserializeObject<PickedUp>(json);
                        break;
                    case "Delivered":
                        eventObj = JsonConvert.DeserializeObject<Delivered>(json);
                        break;
                    default:
                        throw new InvalidOperationException($"Unknown Event: {resolvedEvent.Event.EventType}");
                }

                base.RaiseEvent(eventObj);
            }

            await ConfirmEvents();
        }

        public override Task OnDeactivateAsync()
        {
            _logger.LogDebug("OnDeactivateAsync");

            return base.OnDeactivateAsync();
        }

        private async Task RaiseEvent<T>(string eventName, T raisedEvent)
        {
            var json = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(raisedEvent));
            var eventData = new EventData(Uuid.NewUuid(), eventName, json);

            await _esClient.AppendToStreamAsync(_stream, Convert.ToUInt64(Version - 1),
                new List<EventData> {eventData});

            base.RaiseEvent(raisedEvent);
            await ConfirmEvents();
        }

        public async Task Pickup()
        {
            if (State.Status == TransitStatus.InTransit)
            {
                throw new InvalidOperationException("Shipment has already been picked up.");
            }

            if (State.Status == TransitStatus.Delivered)
            {
                throw new InvalidOperationException("Shipment has already been delivered.");
            }

            await RaiseEvent("PickedUp", new PickedUp(DateTime.UtcNow));
            await ConfirmEvents();
        }

        public async Task Deliver()
        {
            if (State.Status == TransitStatus.AwaitingPickup)
            {
                throw new InvalidOperationException("Shipment has not yet been picked up.");
            }

            if (State.Status == TransitStatus.Delivered)
            {
                throw new InvalidOperationException("Shipment has already been delivered.");
            }

            await RaiseEvent("Delivered", new Delivered(DateTime.UtcNow));
            await ConfirmEvents();
        }

        public Task ForceDeactivateForDemo()
        {
            DeactivateOnIdle();
            return Task.CompletedTask;
        }

        public Task<TransitStatus> GetStatus()
        {
            return Task.FromResult(State.Status);
        }
    }
}
