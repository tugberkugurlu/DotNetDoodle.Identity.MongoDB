using System;

namespace DotNetDoodle.Identity.MongoDB.Entities
{
    public abstract class MongoUserContactRecord : IEquatable<MongoUserEmail>
    {
        protected MongoUserContactRecord(string value)
        {
            Value = value;
        }

        public string Value { get; private set; }
        public ConfirmationRecord ConfirmationRecord { get; private set; }

        public bool IsConfirmed()
        {
            return ConfirmationRecord != null;
        }

        internal void SetConfirmed()
        {
            SetConfirmed(new ConfirmationRecord());
        }

        internal void SetConfirmed(ConfirmationRecord confirmationRecord)
        {
            if (ConfirmationRecord == null)
            {
                ConfirmationRecord = confirmationRecord;
            }
        }

        internal void SetUnconfirmed()
        {
            ConfirmationRecord = null;
        }

        public bool Equals(MongoUserEmail other)
        {
            return other.Value.Equals(Value);
        }
    }
}