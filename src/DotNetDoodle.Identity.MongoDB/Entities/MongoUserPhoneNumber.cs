using System;
using MongoDB.Bson.Serialization.Attributes;

namespace DotNetDoodle.Identity.MongoDB.Entities
{
    public class MongoUserPhoneNumber : MongoUserContactRecord
    {
        [BsonConstructor]
        public MongoUserPhoneNumber() : base(null)
        {
        }
        
        public MongoUserPhoneNumber(string phoneNumber) : base(phoneNumber)
        {
            if (phoneNumber == null) throw new ArgumentNullException("phoneNumber");
        }
    }
}