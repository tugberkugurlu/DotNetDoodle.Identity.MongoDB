using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using DotNetDoodle.Identity.MongoDB.Entities;
using Microsoft.AspNet.Identity;
using MongoDB.Driver;
using MongoDB.Driver.Builders;
using MongoDB.Driver.Linq;

namespace DotNetDoodle.Identity.MongoDB
{
    public class MongoUserStore<TUser> : IUserStore<TUser>,
        IUserLoginStore<TUser>,
        IUserClaimStore<TUser>,
        IUserPasswordStore<TUser>,
        IUserSecurityStampStore<TUser>,
        IQueryableUserStore<TUser>,
        IUserTwoFactorStore<TUser, string>,
        IUserLockoutStore<TUser, string>,
        IUserEmailStore<TUser>,
        IUserPhoneNumberStore<TUser> where TUser : MongoIdentityUser
    {
        private readonly MongoCollection<TUser> _mongoCollection;

        public MongoUserStore(MongoCollection<TUser> mongoCollection)
        {
            if (mongoCollection == null)
            {
                throw new ArgumentNullException("mongoCollection");
            }

            _mongoCollection = mongoCollection;
            EnsureUniqueIndexes();
        }

        // IQueryableUserStore

        public IQueryable<TUser> Users
        {
            get { return _mongoCollection.AsQueryable(); }
        }

        // IUserStore

        public Task CreateAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            Execute(() => _mongoCollection.Insert(user));
            return Task.FromResult(0);
        }

        public Task UpdateAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            Execute(() => _mongoCollection.Save(user));

            return Task.FromResult(0);
        }

        public Task DeleteAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            IMongoQuery removeQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            Execute(() => _mongoCollection.Remove(removeQuery, RemoveFlags.Single));

            return Task.FromResult(0);
        }

        public Task<TUser> FindByIdAsync(string userId)
        {
            TUser user = _mongoCollection.FindOneByIdAs<TUser>(userId);
            return Task.FromResult(user);
        }

        public Task<TUser> FindByNameAsync(string userName)
        {
            return FindByIdAsync(MongoIdentityUser.GenerateId(userName));
        }

        // IUserLoginStore

        public Task AddLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            // NOTE: Not the best way to ensure uniquness.
            if (user.Logins.Any(x => x.Equals(login)))
            {
                throw new InvalidOperationException("Login already exists.");
            }

            MongoUserLogin newLogin = new MongoUserLogin(login);
            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Push(usr => usr.Logins, newLogin);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.AddLogin(newLogin);

            return Task.FromResult(0);
        }

        public Task RemoveLoginAsync(TUser user, UserLoginInfo login)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (login == null) throw new ArgumentNullException("login");

            if (user.Logins.Any(x => x.Equals(login)))
            {
                MongoUserLogin loginToRemove = new MongoUserLogin(login);
                IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
                UpdateBuilder<TUser> updateStatement = Update<TUser>.Pull(usr => usr.Logins, loginToRemove);
                Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
                user.RemoveLogin(loginToRemove);
            }

            return Task.FromResult(0);
        }

        public Task<IList<UserLoginInfo>> GetLoginsAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult<IList<UserLoginInfo>>(user.Logins.Select(login => 
                new UserLoginInfo(login.LoginProvider, login.ProviderKey)).ToList());
        }

        public Task<TUser> FindAsync(UserLoginInfo login)
        {
            if (login == null) throw new ArgumentNullException("login");

            IMongoQuery findQuery = Query.ElemMatch("Logins", 
                Query.And(
                    Query.EQ("LoginProvider", login.LoginProvider),
                    Query.EQ("ProviderKey", login.ProviderKey)
                ));
            
            TUser result = _mongoCollection.FindOne(findQuery);

            return Task.FromResult(result);
        }

        // IUserClaimStore

        public Task<IList<Claim>> GetClaimsAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult<IList<Claim>>(
                user.Claims.Select(clm => new Claim(clm.ClaimType, clm.ClaimValue)).ToList());
        }

        public Task AddClaimAsync(TUser user, Claim claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");

            MongoUserClaim newClaim = new MongoUserClaim(claim);
            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Push(usr => usr.Claims, newClaim);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.AddClaim(newClaim);

            return Task.FromResult(0);
        }

        public Task RemoveClaimAsync(TUser user, Claim claim)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (claim == null) throw new ArgumentNullException("claim");

            MongoUserClaim userClaim = user.Claims.FirstOrDefault(clm => clm.Equals(claim));
            if (userClaim != null)
            {
                IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
                UpdateBuilder<TUser> updateStatement = Update<TUser>.Pull(usr => usr.Claims, userClaim);
                Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
                user.RemoveClaim(userClaim);
            }

            return Task.FromResult(0);
        }

        // IUserPasswordStore

        public Task SetPasswordHashAsync(TUser user, string passwordHash)
        {
            if (user == null) throw new ArgumentNullException("user");

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.PasswordHash, passwordHash);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.SetPasswordHash(passwordHash);

            return Task.FromResult(0);
        }

        public Task<string> GetPasswordHashAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult<string>(user.PasswordHash);
        }

        public Task<bool> HasPasswordAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult<bool>(user.PasswordHash != null);
        }

        // IUserSecurityStampStore

        public Task SetSecurityStampAsync(TUser user, string stamp)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (stamp == null) throw new ArgumentNullException("stamp");

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.SecurityStamp, stamp);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.SetSecurityStamp(stamp);

            return Task.FromResult(0);
        }

        public Task<string> GetSecurityStampAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult<string>(user.SecurityStamp);
        }

        // IUserTwoFactorStore

        public Task SetTwoFactorEnabledAsync(TUser user, bool enabled)
        {
            if (user == null) throw new ArgumentNullException("user");

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.IsTwoFactorEnabled, enabled);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));

            if (enabled)
            {
                user.EnableTwoFactorAuthentication();
            }
            else
            {
                user.DisableTwoFactorAuthentication();
            }

            return Task.FromResult(0);
        }

        public Task<bool> GetTwoFactorEnabledAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.IsTwoFactorEnabled);
        }

        // IUserEmailStore

        public Task SetEmailAsync(TUser user, string email)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (email == null) throw new ArgumentNullException("email");

            MongoUserEmail userEmail = new MongoUserEmail(email);
            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.Email, userEmail);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.SetEmail(email);

            return Task.FromResult(0);
        }

        public Task<string> GetEmailAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult<string>((user.Email != null) 
                ? user.Email.Value 
                : null);
        }

        public Task<bool> GetEmailConfirmedAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (user.Email == null)
            {
                throw new InvalidOperationException("Cannot get the confirmation status of the e-mail because user doesn't have an e-mail.");
            }

            return Task.FromResult(user.Email.IsConfirmed());
        }

        public Task SetEmailConfirmedAsync(TUser user, bool confirmed)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (user.Email == null)
            {
                throw new InvalidOperationException("Cannot set the confirmation status of the e-mail because user doesn't have an e-mail.");
            }

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);

            if (confirmed)
            {
                ConfirmationRecord confirmationRecord = new ConfirmationRecord();
                UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.Email.ConfirmationRecord, confirmationRecord);
                Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
                user.Email.SetConfirmed(confirmationRecord);
            }
            else
            {
                UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.Email.ConfirmationRecord, null);
                Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
                user.Email.SetUnconfirmed();
            }

            return Task.FromResult(0);
        }

        public Task<TUser> FindByEmailAsync(string email)
        {
            if (email == null) throw new ArgumentNullException("email");

            IMongoQuery query = Query<TUser>.EQ(user => user.Email.Value, email);
            TUser mongoIdentityUser = _mongoCollection.FindOne(query);

            return Task.FromResult(mongoIdentityUser);
        }

        // IUserPhoneNumberStore

        public Task SetPhoneNumberAsync(TUser user, string phoneNumber)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (phoneNumber == null) throw new ArgumentNullException("phoneNumber");

            MongoUserPhoneNumber userPhoneNumber = new MongoUserPhoneNumber(phoneNumber);
            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.PhoneNumber, userPhoneNumber);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.SetPhoneNumber(userPhoneNumber);

            return Task.FromResult(0);
        }

        public Task<string> GetPhoneNumberAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult<string>((user.PhoneNumber != null)
                ? user.PhoneNumber.Value
                : null);
        }

        public Task<bool> GetPhoneNumberConfirmedAsync(TUser user)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (user.PhoneNumber == null)
            {
                throw new InvalidOperationException("Cannot get the confirmation status of the phone number because user doesn't have a phone number.");
            }

            return Task.FromResult(user.PhoneNumber.IsConfirmed());
        }

        public Task SetPhoneNumberConfirmedAsync(TUser user, bool confirmed)
        {
            if (user == null)
            {
                throw new ArgumentNullException("user");
            }

            if (user.Email == null)
            {
                throw new InvalidOperationException("Cannot set the confirmation status of the phone number because user doesn't have a phone number.");
            }

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);

            if (confirmed)
            {
                ConfirmationRecord confirmationRecord = new ConfirmationRecord();
                UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.PhoneNumber.ConfirmationRecord, confirmationRecord);
                Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
                user.PhoneNumber.SetConfirmed(confirmationRecord);
            }
            else
            {
                UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.PhoneNumber.ConfirmationRecord, null);
                Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
                user.PhoneNumber.SetUnconfirmed();
            }

            return Task.FromResult(0);
        }

        // IUserLockoutStore

        public Task<DateTimeOffset> GetLockoutEndDateAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");
            if (user.LockoutEndDate == null) throw new InvalidOperationException("LockoutEndDate has no value.");

            return Task.FromResult(user.LockoutEndDate.Value);
        }

        public Task SetLockoutEndDateAsync(TUser user, DateTimeOffset lockoutEnd)
        {
            if (user == null) throw new ArgumentNullException("user");

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.LockoutEndDate, lockoutEnd);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.LockUntil(lockoutEnd);

            return Task.FromResult(0);
        }

        public Task<int> IncrementAccessFailedCountAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            IMongoSortBy sortBy = SortBy.Null;
            UpdateBuilder<TUser> update = Update<TUser>.Inc(usr => usr.AccessFailedCount, 1);
            FindAndModifyResult result = Execute(() => _mongoCollection.FindAndModify(new FindAndModifyArgs
            {
                Query = updateQuery,
                SortBy = sortBy,
                Update = update,
                VersionReturned = FindAndModifyDocumentVersion.Modified,
                Fields = Fields<TUser>.Include(usr => usr.AccessFailedCount).Exclude(usr => usr.Id)
            }));

            int newCount = result.ModifiedDocument["AccessFailedCount"].AsInt32;

            return Task.FromResult(newCount);
        }

        public Task ResetAccessFailedCountAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.AccessFailedCount, 0);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));
            user.ResetAccessFailedCount();

            return Task.FromResult(0);
        }

        public Task<int> GetAccessFailedCountAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.AccessFailedCount);
        }

        public Task<bool> GetLockoutEnabledAsync(TUser user)
        {
            if (user == null) throw new ArgumentNullException("user");

            return Task.FromResult(user.IsLockoutEnabled);
        }

        public Task SetLockoutEnabledAsync(TUser user, bool enabled)
        {
            if (user == null) throw new ArgumentNullException("user");

            IMongoQuery updateQuery = Query<TUser>.EQ(u => u.Id, user.Id);
            UpdateBuilder<TUser> updateStatement = Update<TUser>.Set(usr => usr.IsLockoutEnabled, enabled);
            Execute(() => _mongoCollection.Update(updateQuery, updateStatement));

            if (enabled)
            {
                user.EnableLockout();
            }
            else
            {
                user.DisableLockout();
            }

            return Task.FromResult(0);
        }

        // IDisposable

        public void Dispose()
        {
        }

        // privates

        private void EnsureUniqueIndexes()
        {
            IndexKeysBuilder emailKeyBuilder = new IndexKeysBuilder().Ascending("email.value");
            IndexKeysBuilder loginKeyBuilder = new IndexKeysBuilder().Ascending("logins.loginProvider", "logins.providerKey");

            _mongoCollection.CreateIndex(emailKeyBuilder, new IndexOptionsBuilder().SetUnique(true));
            _mongoCollection.CreateIndex(loginKeyBuilder);
        }

        private TResult Execute<TResult>(Func<TResult> func) where TResult : CommandResult
        {
            TResult result = func();
            if (result.Ok == false)
            {
                throw new MongoException(string.Format(
                    "Update operation was unsuccessful. Code: {0}, ErrorMessage: {1}",
                    result.Code,
                    result.ErrorMessage));
            }

            return result;
        }
    }
}