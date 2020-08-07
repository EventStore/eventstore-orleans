using System;

namespace OrleansEventStore
{
    public class ShipmentState
    {
        public TransitStatus Status { get; set; } = TransitStatus.AwaitingPickup;
        public DateTime? LastEventDateTime { get; set; }

        public ShipmentState Apply(PickedUp pickedUpEvent)
        {
            Status = TransitStatus.InTransit;
            LastEventDateTime = pickedUpEvent.DateTime;
            return this;
        }
        
        public ShipmentState Apply(Delivered delivered)
        {
            Status = TransitStatus.Delivered;
            LastEventDateTime = delivered.DateTime;
            return this;
        }
    }
}