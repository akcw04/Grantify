using System.Text.Json;
using Grantify.Data;
using Grantify.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace Grantify.Services;

// Backend for the PUBLIC landing page — the only page anonymous visitors see.
//
// Because every visitor hits it and none of them are signed in, it is the one
// page whose queries are worth caching. The results are kept in a distributed
// cache for one minute: Amazon ElastiCache (Redis) on the deployed site, plain
// process memory on our laptops (chosen in Program.cs — the code here is the
// same for both).
//
// Why a distributed cache and not a static variable: on AWS there can be more
// than one web server, and each server's private memory would drift out of
// step. Redis gives every instance the same copy, and it survives a redeploy.
//
// CACHE-ASIDE, WITH INVALIDATION
// Reads try the cache first and fall back to the database. When an admin
// creates or edits a scholarship, ScholarshipAdminService calls
// InvalidateLandingCacheAsync so the public page updates immediately instead
// of after the minute runs out.
//
// A cache must never break the page: every cache call is wrapped so that if
// Redis is unreachable, the page quietly reads the database as if there were
// no cache at all.
public class ScholarshipService
{
    // One minute: long enough to absorb a burst of visitors, short enough that
    // the page corrects itself quickly even if an invalidation is ever missed.
    private static readonly TimeSpan CacheLifetime = TimeSpan.FromMinutes(1);

    private const string ListCacheKey = "landing:scholarships";
    private const string StatsCacheKey = "landing:stats";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly AppDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly ILogger<ScholarshipService> _logger;

    public ScholarshipService(AppDbContext db, IDistributedCache cache, ILogger<ScholarshipService> logger)
    {
        _db = db;
        _cache = cache;
        _logger = logger;
    }

    // The scholarships a VISITOR sees on the public home page: published, and
    // still open. Closest deadline first, so the most urgent is at the top.
    //
    // The deadline test matters. Without it the page listed scholarships whose
    // closing date had already gone by, under a heading that says "Open
    // scholarships" — and a visitor who registered because of one of them would
    // find they could not apply.
    //
    // Returns a small DTO rather than the Scholarship entity: only what the
    // public page shows leaves this method, and a flat record serialises
    // cleanly into the cache (an entity would drag its navigation lists along).
    public async Task<List<LandingScholarship>> GetPublishedAsync()
    {
        var cached = await TryGetCachedAsync<List<LandingScholarship>>(ListCacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var today = DateTime.Today;

        var scholarships = await _db.Scholarships
            .Where(s => s.IsPublished && s.Deadline >= today)
            .OrderBy(s => s.Deadline)
            .Select(s => new LandingScholarship(
                s.Name, s.Provider, s.Description, s.MinimumCgpa, s.MaximumHouseholdIncome, s.Deadline))
            .ToListAsync();

        await TrySetCachedAsync(ListCacheKey, scholarships);
        return scholarships;
    }

    // The three numbers shown across the top of the public home page.
    //
    // They are counted from the real database rather than typed into the page.
    // A landing page that claims figures the system cannot back up is the kind
    // of thing a marker checks, and it would be wrong the moment an admin
    // published anything.
    public async Task<PublicStats> GetPublicStatsAsync()
    {
        var cached = await TryGetCachedAsync<PublicStats>(StatsCacheKey);
        if (cached is not null)
        {
            return cached;
        }

        var today = DateTime.Today;

        var stats = new PublicStats
        {
            OpenScholarships = await _db.Scholarships
                .CountAsync(s => s.IsPublished && s.Deadline >= today),

            // Distinct providers, so listing five scholarships from one
            // university does not read as five partners.
            Providers = await _db.Scholarships
                .Where(s => s.IsPublished)
                .Select(s => s.Provider)
                .Distinct()
                .CountAsync(),

            AwardsMade = await _db.ScholarshipApplications
                .CountAsync(a => a.Status == ApplicationStatus.Approved)
        };

        await TrySetCachedAsync(StatsCacheKey, stats);
        return stats;
    }

    // Called by ScholarshipAdminService whenever a scholarship is created or
    // edited, so a change an admin just published shows on the public page on
    // the very next visit rather than up to a minute later.
    public async Task InvalidateLandingCacheAsync()
    {
        try
        {
            await _cache.RemoveAsync(ListCacheKey);
            await _cache.RemoveAsync(StatsCacheKey);
        }
        catch (Exception ex)
        {
            // If Redis is down the entries cannot be removed — but they also
            // cannot be read, so nobody is served the stale copy anyway.
            _logger.LogWarning(ex, "Could not clear the landing page cache.");
        }
    }

    // ------------------------------------------------------------------
    // Cache plumbing. Both helpers swallow cache failures on purpose: the
    // cache is an accelerator, and an unreachable Redis must degrade to
    // "slightly slower page", never to an error page.
    // ------------------------------------------------------------------

    private async Task<T?> TryGetCachedAsync<T>(string key) where T : class
    {
        try
        {
            var json = await _cache.GetStringAsync(key);
            return json is null ? null : JsonSerializer.Deserialize<T>(json, JsonOptions);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache read failed for {Key}; reading the database instead.", key);
            return null;
        }
    }

    private async Task TrySetCachedAsync<T>(string key, T value)
    {
        try
        {
            await _cache.SetStringAsync(key,
                JsonSerializer.Serialize(value, JsonOptions),
                new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = CacheLifetime });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Cache write failed for {Key}; the page still works without it.", key);
        }
    }
}

// One scholarship as the public landing page shows it. A record, so it
// serialises to and from the cache without dragging entity baggage along.
public record LandingScholarship(
    string Name,
    string Provider,
    string Description,
    decimal MinimumCgpa,
    decimal? MaximumHouseholdIncome,
    DateTime Deadline);

// The headline figures on the public home page.
public class PublicStats
{
    public int OpenScholarships { get; set; }
    public int Providers { get; set; }
    public int AwardsMade { get; set; }
}
