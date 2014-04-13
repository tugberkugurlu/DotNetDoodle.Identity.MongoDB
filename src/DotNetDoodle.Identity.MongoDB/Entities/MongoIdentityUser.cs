using System;
using System.Collections.Generic;
using System.Globalization;
using System.Security.Claims;
using Microsoft.AspNet.Identity;
using MongoDB.Bson.Serialization.Attributes;

namespace DotNetDoodle.Identity.MongoDB.Entities
{
    public class MongoIdentityUser : IUser<string>
    {
        private List<MongoUserClaim> _claims;
        private List<MongoUserLogin> _logins;

        [BsonConstructor]
        private MongoIdentityUser()
        {
        }

        public MongoIdentityUser(string userName)
        {
            if (userName == null)
            {
                throw new ArgumentNullException("userName");
            }

            Id = GenerateId(userName);
            UserName = userName;
            _claims = new List<MongoUserClaim>();
            _logins = new List<MongoUserLogin>();
        }

        public MongoIdentityUser(string userName, string email)
            : this(userName)
        {
            if (email == null)
            {
                throw new ArgumentNullException("email");
            }

            Email = new MongoUserEmail(email);
        }

        [BsonId]
        public string Id { get; private set; }
        public string UserName { get; set; }
        public MongoUserEmail Email { get; private set; }

        public MongoUserPhoneNumber PhoneNumber { get; private set; }
        public string PasswordHash { get; private set; }
        public string SecurityStamp { get; private set; }
        public bool IsLockoutEnabled { get; private set; }
        public bool IsTwoFactorEnabled { get; private set; }

        public IEnumerable<MongoUserClaim> Claims
        {
            get
            {
                return _claims;
            }

            private set
            {
                if (_claims == null)
                {
                    _claims = new List<MongoUserClaim>();
                }

                _claims.AddRange(value);
            }
        }
        public IEnumerable<MongoUserLogin> Logins
        {
            get
            {
                return _logins;
            }

            private set
            {
                if (_logins == null)
                {
                    _logins = new List<MongoUserLogin>();
                }

                _logins.AddRange(value);
            }
        }

        public int AccessFailedCount { get; private set; }
        public DateTimeOffset? LockoutEndDate { get; private set; }

        public virtual void EnableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = true;
        }

        public virtual void DisableTwoFactorAuthentication()
        {
            IsTwoFactorEnabled = false;
        }

        public virtual void EnableLockout()
        {
            IsLockoutEnabled = true;
        }

        public virtual void DisableLockout()
        {
            IsLockoutEnabled = false;
        }

        public virtual void SetEmail(string email)
        {
            SetEmail(new MongoUserEmail(email));
        }

        public virtual void SetEmail(MongoUserEmail mongoUserEmail)
        {
            Email = mongoUserEmail;
        }

        public virtual void SetPhoneNumber(string phoneNumber)
        {
            SetPhoneNumber(new MongoUserPhoneNumber(phoneNumber));
        }

        public virtual void SetPhoneNumber(MongoUserPhoneNumber mongoUserPhoneNumber)
        {
            PhoneNumber = mongoUserPhoneNumber;
        }

        public virtual void SetPasswordHash(string passwordHash)
        {
            PasswordHash = passwordHash;
        }

        public virtual void SetSecurityStamp(string securityStamp)
        {
            SecurityStamp = securityStamp;
        }

        public virtual void IncrementAccessFailedCount()
        {
            AccessFailedCount++;
        }

        public virtual void SetAccessFailedCount(int accessFailedCount)
        {
            AccessFailedCount = accessFailedCount;
        }

        public virtual void ResetAccessFailedCount()
        {
            AccessFailedCount = 0;
        }

        public virtual void LockUntil(DateTimeOffset lockoutEndDate)
        {
            LockoutEndDate = lockoutEndDate;
        }

        public virtual void AddClaim(Claim claim)
        {
            if (claim == null)
            {
                throw new ArgumentNullException("claim");
            }

            AddClaim(new MongoUserClaim(claim));
        }

        public virtual void AddClaim(MongoUserClaim mongoUserClaim)
        {
            if (mongoUserClaim == null)
            {
                throw new ArgumentNullException("mongoUserClaim");
            }

            _claims.Add(mongoUserClaim);
        }

        public virtual void RemoveClaim(MongoUserClaim mongoUserClaim)
        {
            if (mongoUserClaim == null)
            {
                throw new ArgumentNullException("mongoUserClaim");
            }

            _claims.Remove(mongoUserClaim);
        }

        public virtual void AddLogin(MongoUserLogin mongoUserLogin)
        {
            if (mongoUserLogin == null)
            {
                throw new ArgumentNullException("mongoUserLogin");
            }

            _logins.Add(mongoUserLogin);
        }

        public virtual void RemoveLogin(MongoUserLogin mongoUserLogin)
        {
            if (mongoUserLogin == null)
            {
                throw new ArgumentNullException("mongoUserLogin");
            }

            _logins.Remove(mongoUserLogin);
        }

        public static string GenerateId(string userName)
        {
            return userName.ToLower(CultureInfo.InvariantCulture);
        }
    }
}