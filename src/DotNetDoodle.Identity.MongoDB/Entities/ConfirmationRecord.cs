using System;

namespace DotNetDoodle.Identity.MongoDB.Entities
{
    public class ConfirmationRecord
    {
        public ConfirmationRecord()
            : this(DateTimeOffset.UtcNow)
        {
        }

        public ConfirmationRecord(DateTimeOffset confirmedOn)
        {
            ConfirmedOn = confirmedOn;
        }

        public DateTimeOffset ConfirmedOn { get; private set; }
    }
}