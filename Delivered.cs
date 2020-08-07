using System;

namespace OrleansEventStore
{
    public class Delivered
    {
        public Delivered(DateTime dateTime)
        {
            DateTime = dateTime;
        }
        
        public DateTime DateTime { get; }
    }
}