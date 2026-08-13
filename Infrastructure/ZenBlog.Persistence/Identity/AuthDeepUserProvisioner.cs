using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Domain.Entities;
using ZenBlog.Persistence.Context;

namespace ZenBlog.Persistence.Identity
{
    /// <summary>
    /// Keeps the local AppUser table in step with AuthDeep, using the AuthDeep subject id
    /// as the primary key.
    ///
    /// Three cases, in the order they are checked:
    ///
    ///   already keyed   The row exists under the AuthDeep id. Nothing to do — the common
    ///                   case, and the one the cache is there to make free.
    ///
    ///   legacy row      A row with the same email but a locally generated id, created
    ///                   before this integration. Re-keyed in place so their blogs and
    ///                   comments follow them, which the ON UPDATE CASCADE constraints
    ///                   added in authdeep_user_rekey_cascade do in one statement.
    ///
    ///   new reader      No row at all. Created with the AuthDeep id.
    /// </summary>
    public sealed class AuthDeepUserProvisioner(
        AppDbContext context,
        IMemoryCache cache,
        ILogger<AuthDeepUserProvisioner> logger) : IAuthDeepUserProvisioner
    {
        /// <summary>
        /// How long a provisioned id is remembered. Only suppresses a primary-key lookup,
        /// so the cost of it being wrong is one extra query after a restart — never a
        /// missing row, because every write path still fails loudly on a bad FK.
        /// </summary>
        private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(30);

        public async Task EnsureLocalUserAsync(
            AuthDeepUserDescriptor descriptor,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(descriptor.UserId))
            {
                return;
            }

            var cacheKey = $"authdeep:user:{descriptor.UserId}";
            if (cache.TryGetValue(cacheKey, out _))
            {
                return;
            }

            if (await context.Users.AnyAsync(u => u.Id == descriptor.UserId, cancellationToken))
            {
                cache.Set(cacheKey, true, CacheLifetime);
                return;
            }

            if (!await TryRekeyLegacyUserAsync(descriptor, cancellationToken))
            {
                await CreateUserAsync(descriptor, cancellationToken);
            }

            cache.Set(cacheKey, true, CacheLifetime);
        }

        /// <summary>
        /// Moves a pre-AuthDeep row onto its AuthDeep id, matched on email.
        ///
        /// Email is the only stable link between the two systems: the reader signed up
        /// here with an address and later authenticated at AuthDeep with the same one.
        /// The comparison uses NormalizedEmail so it is case-insensitive in the same way
        /// Identity's own lookups are.
        /// </summary>
        private async Task<bool> TryRekeyLegacyUserAsync(
            AuthDeepUserDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(descriptor.Email))
            {
                return false;
            }

            // Re-keying is a raw UPDATE that leans on ON UPDATE CASCADE, neither of which
            // the in-memory provider used by the Testing host supports. There, a reader
            // with no matching id is simply created fresh.
            if (!context.Database.IsRelational())
            {
                return false;
            }

            var normalizedEmail = descriptor.Email.ToUpperInvariant();
            var legacy = await context.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);

            if (legacy is null)
            {
                return false;
            }

            // One statement, and the FK constraints carry it into Blogs, Comments,
            // RefreshTokens and the four Identity tables. Wrapped anyway so a failure
            // partway cannot leave rows split across two ids.
            await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);

            await context.Database.ExecuteSqlAsync(
                $"""UPDATE "AspNetUsers" SET "Id" = {descriptor.UserId} WHERE "Id" = {legacy.Id}""",
                cancellationToken);

            // ActivityLogs records the actor as a plain string with no foreign key, so it
            // is not covered by the cascade and would otherwise point at a dead id.
            await context.Database.ExecuteSqlAsync(
                $"""UPDATE "ActivityLogs" SET "ActorUserId" = {descriptor.UserId} WHERE "ActorUserId" = {legacy.Id}""",
                cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            logger.LogInformation(
                "Re-keyed local user {LegacyId} to AuthDeep subject {AuthDeepId}",
                legacy.Id,
                descriptor.UserId);

            return true;
        }

        /// <summary>
        /// Creates a row for a reader AuthDeep knows and this service does not.
        ///
        /// The gateway sends an id, an email and roles — no name — so the display fields
        /// are seeded from the email local part and the reader can correct them on their
        /// profile page. Roles are deliberately not written here: authorization reads the
        /// gateway-injected roles off the request, and a local copy would be a second
        /// source of truth that drifts.
        /// </summary>
        private async Task CreateUserAsync(
            AuthDeepUserDescriptor descriptor,
            CancellationToken cancellationToken)
        {
            var email = descriptor.Email;
            var localPart = string.IsNullOrWhiteSpace(email)
                ? descriptor.UserId
                : email.Split('@')[0];

            var user = new AppUser
            {
                Id = descriptor.UserId,
                UserName = await UniqueUserNameAsync(localPart, descriptor.UserId, cancellationToken),
                NormalizedUserName = null,
                Email = email,
                NormalizedEmail = email?.ToUpperInvariant(),
                // AuthDeep verified the address; that is what let them sign in at all.
                EmailConfirmed = email is not null,
                FirstName = localPart,
                LastName = string.Empty,
                SecurityStamp = Guid.NewGuid().ToString(),
                ConcurrencyStamp = Guid.NewGuid().ToString()
            };

            user.NormalizedUserName = user.UserName!.ToUpperInvariant();

            context.Users.Add(user);
            await context.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Provisioned local user for AuthDeep subject {AuthDeepId}",
                descriptor.UserId);
        }

        /// <summary>
        /// Usernames carry a unique index, and two AuthDeep readers can easily share an
        /// email local part across different domains. On a collision the name is suffixed
        /// with the head of the subject id, which is unique by construction.
        /// </summary>
        private async Task<string> UniqueUserNameAsync(
            string preferred,
            string userId,
            CancellationToken cancellationToken)
        {
            var candidate = preferred;
            var normalized = candidate.ToUpperInvariant();

            if (!await context.Users.AnyAsync(u => u.NormalizedUserName == normalized, cancellationToken))
            {
                return candidate;
            }

            var suffix = userId.Replace("-", string.Empty);
            candidate = $"{preferred}-{suffix[..Math.Min(8, suffix.Length)]}";
            normalized = candidate.ToUpperInvariant();

            if (!await context.Users.AnyAsync(u => u.NormalizedUserName == normalized, cancellationToken))
            {
                return candidate;
            }

            // Two readers sharing both a local part and an id prefix is vanishingly
            // unlikely; the whole id removes any doubt.
            return $"{preferred}-{suffix}";
        }
    }
}
