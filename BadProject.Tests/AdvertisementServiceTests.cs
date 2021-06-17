using System;
using System.Collections.Generic;
using System.Runtime.Caching;
using Adv;
using AutoFixture;
using BadProject;
using BadProject.Providers;
using BadProject.Services;
using BadProject.Settings;
using FluentAssertions;
using NSubstitute;
using ThirdParty;
using Xunit;

namespace BadProjectTests
{
    public class AdvertisementServiceTests
    {
        private readonly AdvertisementService Service;
        private readonly IDateTimeProvider _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        private readonly ISqlAdvProvider _sqlAdvProvider = Substitute.For<ISqlAdvProvider>();

        // private readonly NoSqlAdvProvider _noSqlAdvProviderBase = Substitute.For<NoSqlAdvProvider>();
        private readonly INoSqlAdvProvider _noSqlAdvProvider = Substitute.For<INoSqlAdvProvider>();

        private readonly AppSettings _appSettings = new AppSettings
            {RetryCount = 3, RetryDelay = 1000, ErrorThreshold = 3};

        private MemoryCache _cache = new MemoryCache("TEST");
        private ErrorService _errorService = new ErrorService {HttpErrors = new Queue<DateTime>()};

        private readonly IFixture _fixture = new Fixture();

        public AdvertisementServiceTests()
        {
            Service = new AdvertisementService(_dateTimeProvider, _noSqlAdvProvider, _sqlAdvProvider, _appSettings,
                _cache, _errorService);
        }

        [Fact]
        public void GetAdvertisement_ShouldGetSqlAd()
        {
            // Arrange
            const string advId = "1";
            var adv = _fixture.Build<Advertisement>().With(x => x.WebId, advId).Create();
            _dateTimeProvider.DateTimeNow.Returns(new DateTime(2021, 6, 16));
            _sqlAdvProvider.GetAdv(Arg.Any<string>()).Returns(adv);

            // Act
            var result = Service.GetAdvertisement(advId);

            // Assert
            result.Should().Be(adv);
        }

        [Fact]
        public void GetAdvertisement_ShouldGetHttpAd()
        {
            // Arrange
            const string advId = "1";
            var adv = _fixture.Build<Advertisement>().With(x => x.WebId, advId).Create();
            _dateTimeProvider.DateTimeNow.Returns(new DateTime(2021, 6, 16));
            _noSqlAdvProvider.GetAdv(Arg.Any<string>()).Returns(adv);

            // Act
            var result = Service.GetAdvertisement(advId);

            // Assert
            result.Should().Be(adv);
        }

        [Fact]
        public void GetAdvertisement_ShouldGetCacheAd()
        {
            // Arrange
            const string id = "1";
            var adv = _fixture.Build<Advertisement>().With(x => x.WebId, id).Create();

            _cache.Set($"AdvKey_{id}", adv, DateTimeOffset.Now.AddMinutes(1));

            _dateTimeProvider.DateTimeNow.Returns(new DateTime(2021, 6, 16));
            _dateTimeProvider.DateTimeOffsetNow.Returns(DateTime.Now);

            // Act
            var result = Service.GetAdvertisement(id);

            // Assert
            result.Should().Be(adv);
        }

        [Fact]
        public void GetAdvertisement_ShouldReturnNull_WhenAllProvidersAreEmpty()
        {
            // Arrange
            const string id = "1";

            _dateTimeProvider.DateTimeNow.Returns(new DateTime(2021, 6, 16));
            _dateTimeProvider.DateTimeOffsetNow.Returns(DateTime.Now);

            // Act
            var result = Service.GetAdvertisement(id);

            // Assert
            result.Should().Be(null);
        }

        [Fact]
        public void GetAdvertisement_ShouldReturnNull_WhenThereAreTooManyHttpErrors()
        {
            // Arrange
            const string id = "1";
            DateTime dateTime = new DateTime(2021, 6, 16, 10, 0, 0);
            _dateTimeProvider.DateTimeNow.Returns(dateTime);
            _dateTimeProvider.DateTimeOffsetNow.Returns(DateTime.Now);

            for (var i = 0; i < _appSettings.ErrorThreshold + 1; i++)
            {
                _errorService.HttpErrors.Enqueue(dateTime.AddMinutes(-10));
            }

            var adv = _fixture.Build<Advertisement>().With(x => x.WebId, id).Create();
            _noSqlAdvProvider.GetAdv(Arg.Any<string>()).Returns(adv);

            // Act
            var result = Service.GetAdvertisement(id);

            // Assert
            result.Should().Be(null);
        }
    }
}