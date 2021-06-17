using System;
using System.Collections.Generic;
using System.Configuration;
using System.Runtime.Caching;
using System.Threading;
using BadProject.Providers;
using BadProject.Services;
using BadProject.Settings;
using ThirdParty;

namespace Adv
{
    public class AdvertisementService
    {
        private readonly IDateTimeProvider _dateTimeProvider;
        private readonly INoSqlAdvProvider _noSqlAdvProvider;
        private readonly ISqlAdvProvider _sqlAdvProvider;
        private readonly AppSettings _appSettings;

        private readonly int RetryDelay;
        private readonly int RetryCount;
        private DateTimeOffset cacheOffset;
        private readonly int ErrorThreshold;

        private static MemoryCache cache;
        private static Queue<DateTime> errors;

        private Object lockObj = new Object();

        public AdvertisementService(IDateTimeProvider dateTimeProvider, INoSqlAdvProvider noSqlAdvProvider,
            ISqlAdvProvider sqlAdvProvider, AppSettings appSettings, MemoryCache memoryCache, ErrorService errorService)
        {
            _dateTimeProvider = dateTimeProvider;
            _noSqlAdvProvider = noSqlAdvProvider;
            _sqlAdvProvider = sqlAdvProvider;
            RetryCount = appSettings.RetryCount;
            RetryDelay = appSettings.RetryDelay;
            ErrorThreshold = appSettings.ErrorThreshold;
            cache = memoryCache;
            errors = errorService.HttpErrors;
            cacheOffset = dateTimeProvider.DateTimeOffsetNow.AddMinutes(5);
        }

        public AdvertisementService() : this(new DateTimeProvider(), new NoSqlAdvProviderProxy(new NoSqlAdvProvider()),
            new SqlAdvProviderProxy(),
            new AppSettings
            {
                RetryCount = int.Parse(ConfigurationManager.AppSettings["RetryDelay"]),
                RetryDelay = int.Parse(ConfigurationManager.AppSettings["RetryDelay"])
            }, new MemoryCache("default"),
            new ErrorService {HttpErrors = new Queue<DateTime>()})
        {
        }

        // **************************************************************************************************
        // Loads Advertisement information by id
        // from cache or if not possible uses the "mainProvider" or if not possible uses the "backupProvider"
        // **************************************************************************************************
        // Detailed Logic:
        // 
        // 1. Tries to use cache (and retuns the data or goes to STEP2)
        //
        // 2. If the cache is empty it uses the NoSqlDataProvider (mainProvider), 
        //    in case of an error it retries it as many times as needed based on AppSettings
        //    (returns the data if possible or goes to STEP3)
        //
        // 3. If it can't retrive the data or the ErrorCount in the last hour is more than 10, 
        //    it uses the SqlDataProvider (backupProvider)
        public Advertisement GetAdvertisement(string id)
        {
            lock (lockObj)
            {
                // Try Cache if available
                var adv = GetCacheAdvertisement(id);
                if (adv != null) return adv;


                // Try HTTP Provider
                adv = GetHttpAdvertisement(id);
                if (adv != null) return adv;


                // finally Try Backup provider
                adv = GetSqlAdvertisement(id);
                if (adv != null) AddAdvertisementToCache(adv);

                return adv;
            }
        }
        private Advertisement GetCacheAdvertisement(string id)
        {
            return (Advertisement) cache.Get($"AdvKey_{id}");
        }
        private bool TooManyErrors(int threshold)
        {
            while (errors.Count > threshold) errors.Dequeue();
            return errors.Count > 0 && errors.Dequeue() > _dateTimeProvider.DateTimeNow.AddHours(-1);
        }
        private Advertisement GetHttpAdvertisement(string id)
        {
            Advertisement adv;
            if (!TooManyErrors(ErrorThreshold))
            {
                int retry = 0;
                do
                {
                    retry++;
                    try
                    {
                        adv = _noSqlAdvProvider.GetAdv(id);
                        if (adv == null) continue;
                        AddAdvertisementToCache(adv);
                        return adv;
                    }
                    catch
                    {
                        Thread.Sleep(RetryDelay);
                        errors.Enqueue(_dateTimeProvider.DateTimeNow); // Store HTTP error timestamp              
                    }
                } while (retry < RetryCount);
            }

            return null;
        }
        private Advertisement GetSqlAdvertisement(string id)
        {
            return _sqlAdvProvider.GetAdv(id);
        }
        private void AddAdvertisementToCache(Advertisement advertisement)
        {
            cache.Set($"AdvKey_{advertisement.WebId}", advertisement, cacheOffset);
        }
    }
}