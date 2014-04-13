using System;
using Microsoft.AspNet.Identity;
using MongoDB.Bson.Serialization.Attributes;

namespace DotNetDoodle.Identity.MongoDB.Entities
{
    public class MongoUserLogin : IEquatable<MongoUserLogin>, IEquatable<UserLoginInfo>
    {

        [BsonConstructor]
        private MongoUserLogin()
        {
        }

        public MongoUserLogin(UserLoginInfo loginInfo)
        {
            if (loginInfo == null) throw new ArgumentNullException("loginInfo");

            LoginProvider = loginInfo.LoginProvider;
            ProviderKey = loginInfo.ProviderKey;
        }

        public string LoginProvider { get; private set; }
        public string ProviderKey { get; private set; }

        public bool Equals(MongoUserLogin other)
        {
            return (other.LoginProvider.Equals(LoginProvider) && other.ProviderKey.Equals(ProviderKey));
        }

        public bool Equals(UserLoginInfo other)
        {
            return (other.LoginProvider.Equals(LoginProvider) && other.ProviderKey.Equals(ProviderKey));
        }
    }
}