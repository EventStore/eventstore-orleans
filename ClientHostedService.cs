using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Orleans;

namespace OrleansEventStore
{
    public class ClientHostedService : IHostedService
    {
        private readonly IClusterClient _client;
        private readonly ILogger<ClientHostedService> _logger;

        public ClientHostedService(IClusterClient client, ILogger<ClientHostedService> logger)
        {
            _client = client;
            _logger = logger;
        }
        
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var shipmentId = Guid.NewGuid();
            
            // Get a Shipment Grain based on a new GUID for demo
            var shipment = _client.GetGrain<IShipment>(shipmentId);
            _logger.LogInformation($"{shipmentId} {await shipment.GetStatus()}");
            
            await shipment.Pickup();
            _logger.LogInformation($"{shipmentId} {await shipment.GetStatus()}");
            
            await shipment.Deliver();
            _logger.LogInformation($"{shipmentId} {await shipment.GetStatus()}");

            // For demo purposes, deactivate the grain
            await shipment.ForceDeactivateForDemo();
            
            // Fetch grain again to force it to re-activate and re-hydrate using event stream
            shipment = _client.GetGrain<IShipment>(shipmentId);
            _logger.LogInformation($"{shipmentId} {await shipment.GetStatus()}");
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}