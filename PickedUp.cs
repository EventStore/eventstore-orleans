using System;

namespace OrleansEventStore
{
    public class PickedUp
    {
        public PickedUp(DateTime dateTime)
        {
            DateTime = dateTime;
        }
        
        public DateTime DateTime { get; }
    }
}