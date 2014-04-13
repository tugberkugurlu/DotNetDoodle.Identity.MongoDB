using System;
using System.Security.Claims;
using MongoDB.Bson.Serialization.Attributes;

namespace DotNetDoodle.Identity.MongoDB.Entities
{
    public class MongoUserClaim : IEquatable<MongoUserClaim>, IEquatable<Claim>
    {
        [BsonConstructor]
        private MongoUserClaim()
        {
        }

        public MongoUserClaim(Claim claim)
        {
            if (claim == null) throw new ArgumentNullException("claim");

            ClaimType = claim.Type;
            ClaimValue = claim.Value;
        }

        public MongoUserClaim(string claimType, string claimValue)
        {
            if (claimType == null) throw new ArgumentNullException("claimType");
            if (claimValue == null) throw new ArgumentNullException("claimValue");

            ClaimType = claimType;
            ClaimValue = claimValue;
        }

        public string ClaimType { get; private set; }
        public string ClaimValue { get; private set; }

        public bool Equals(MongoUserClaim other)
        {
            return (other.ClaimType.Equals(ClaimType) && other.ClaimValue.Equals(ClaimValue));
        }

        public bool Equals(Claim other)
        {
            return (other.Type.Equals(ClaimType) && other.Value.Equals(ClaimValue));
        }
    }
}
