using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Moq;
using VirtoCommerce.CustomerModule.Core.Services;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Pages.Core.Search;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Platform.Core.Settings;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.StoreModule.Core.Services;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Data.Queries;
using Xunit;
using static VirtoCommerce.Xapi.Core.ModuleConstants;

namespace VirtoCommerce.XCMS.Tests.Handlers
{
    public class GetPageDocumentAlternatesQueryHandlerTests
    {
        [Fact]
        public async Task Handle_ReturnsOnePublishedTranslationPerLanguage()
        {
            // Arrange
            PageDocumentSearchCriteria criteria = null;
            var search = new Mock<IPageDocumentSearchService>();
            search
                .Setup(x => x.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), It.IsAny<bool>()))
                .Callback<PageDocumentSearchCriteria, bool>((c, _) => criteria = c)
                .ReturnsAsync(new PageDocumentSearchResult
                {
                    TotalCount = 3,
                    Results =
                    [
                        new PageDocument { Id = "nl", CultureName = "nl-NL", Permalink = "/veelgestelde-vragen" },
                        new PageDocument { Id = "en", CultureName = "en-US", Permalink = "/faq" },
                        new PageDocument { Id = "en-2", CultureName = "en-US", Permalink = "/faq" },
                    ],
                });

            // Act
            var result = await CreateHandler(search.Object).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq", OrganizationId = "Organization" },
                CancellationToken.None);

            // Assert
            result.Select(x => (x.CultureName, x.Permalink)).Should().Equal(("en-US", "/faq"), ("nl-NL", "/veelgestelde-vragen"));
            criteria.StoreId.Should().Be("Store");
            criteria.OrganizationId.Should().Be("Organization");
            criteria.TranslationGroupIds.Should().Equal("faq");
            criteria.Status.Should().Be(PageDocumentStatus.Published);
            criteria.CertainDate.Should().NotBeNull();
        }

        [Fact]
        public async Task Handle_WithoutTranslationGroup_ReturnsEmptyWithoutSearching()
        {
            // Arrange
            var search = new Mock<IPageDocumentSearchService>(MockBehavior.Strict);

            // Act
            var result = await CreateHandler(search.Object).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store" },
                CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_SkipsLanguagesTheStoreDoesNotServe()
        {
            // Arrange: de-DE was removed from the store, its published version is still in the index.
            var search = SearchReturning(
                new PageDocument { Id = "en", CultureName = "en-US", Permalink = "/faq" },
                new PageDocument { Id = "de", CultureName = "de-DE", Permalink = "/haeufige-fragen" });

            // Act
            var result = await CreateHandler(search).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq" },
                CancellationToken.None);

            // Assert
            result.Select(x => x.CultureName).Should().Equal("en-US");
        }

        [Fact]
        public async Task Handle_SeveralVersionsOfALanguage_TakesTheLatest()
        {
            // Arrange
            var search = SearchReturning(
                new PageDocument { Id = "en-old", CultureName = "en-US", Permalink = "/faq-old", ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PageDocument { Id = "en-new", CultureName = "en-US", Permalink = "/faq", ModifiedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) });

            // Act
            var result = await CreateHandler(search).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq" },
                CancellationToken.None);

            // Assert
            result.Select(x => x.Permalink).Should().Equal("/faq");
        }

        [Fact]
        public async Task Handle_SeveralVersionsOfALanguage_ComparesCreatedDateOfNeverModifiedVersion()
        {
            // Arrange: the never modified version was created after the other one was last modified.
            var search = SearchReturning(
                new PageDocument
                {
                    Id = "en-old",
                    CultureName = "en-US",
                    Permalink = "/faq-old",
                    CreatedDate = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                },
                new PageDocument
                {
                    Id = "en-new",
                    CultureName = "en-US",
                    Permalink = "/faq",
                    CreatedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc),
                });

            // Act
            var result = await CreateHandler(search).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq" },
                CancellationToken.None);

            // Assert
            result.Select(x => x.Permalink).Should().Equal("/faq");
        }

        [Fact]
        public async Task Handle_SkipsVersionsWithoutPermalink()
        {
            // Arrange: the latest en-US version has no permalink, the older one has.
            var search = SearchReturning(
                new PageDocument { Id = "en-old", CultureName = "en-US", Permalink = "/faq", ModifiedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PageDocument { Id = "en-new", CultureName = "en-US", Permalink = "", ModifiedDate = new DateTime(2026, 6, 1, 0, 0, 0, DateTimeKind.Utc) },
                new PageDocument { Id = "nl", CultureName = "nl-NL", Permalink = null });

            // Act
            var result = await CreateHandler(search).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq" },
                CancellationToken.None);

            // Assert
            result.Select(x => (x.Id, x.Permalink)).Should().Equal(("en-old", "/faq"));
        }

        [Fact]
        public async Task Handle_StoreNotFound_ReturnsEmptyWithoutSearching()
        {
            // Arrange
            var search = new Mock<IPageDocumentSearchService>(MockBehavior.Strict);

            // Act
            var result = await CreateHandler(search.Object, store: null).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Unknown", TranslationGroupId = "faq" },
                CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_PagesDisabledForStore_ReturnsEmptyWithoutSearching()
        {
            // Arrange
            var search = new Mock<IPageDocumentSearchService>(MockBehavior.Strict);

            // Act
            var result = await CreateHandler(search.Object, CreateStore(pagesEnabled: false)).Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq" },
                CancellationToken.None);

            // Assert
            result.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_Anonymous_DoesNotLookUpTheUser()
        {
            // Arrange
            PageDocumentSearchCriteria criteria = null;
            var search = new Mock<IPageDocumentSearchService>();
            search
                .Setup(x => x.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), It.IsAny<bool>()))
                .Callback<PageDocumentSearchCriteria, bool>((c, _) => criteria = c)
                .ReturnsAsync(new PageDocumentSearchResult { Results = [] });
            var dependency = new Mock<IOptionalDependency<IPageDocumentSearchService>>();
            dependency.Setup(x => x.HasValue).Returns(true);
            dependency.Setup(x => x.Value).Returns(search.Object);
            var storeService = new Mock<IStoreService>();
            storeService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync([CreateStore()]);
            var handler = new GetPageDocumentAlternatesQueryHandler(dependency.Object,
                () => throw new InvalidOperationException("The user must not be looked up"), Mock.Of<IMemberService>(), storeService.Object);

            // Act
            await handler.Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq", UserId = AnonymousUser.UserName },
                CancellationToken.None);

            // Assert
            criteria.UserGroups.Should().BeEmpty();
        }

        [Theory]
        [InlineData(null)]
        [InlineData("org")]
        public async Task Handle_FiltersByTheVisitorOrganization(string organizationId)
        {
            // Arrange
            PageDocumentSearchCriteria criteria = null;
            var search = new Mock<IPageDocumentSearchService>();
            search
                .Setup(x => x.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), It.IsAny<bool>()))
                .Callback<PageDocumentSearchCriteria, bool>((c, _) => criteria = c)
                .ReturnsAsync(new PageDocumentSearchResult { Results = [] });
            var handler = CreateHandler(search.Object);

            // Act
            await handler.Handle(
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq", OrganizationId = organizationId, UserId = AnonymousUser.UserName },
                CancellationToken.None);

            // Assert
            criteria.FilterByOrganization.Should().BeTrue();
            criteria.OrganizationId.Should().Be(organizationId);
        }

        private static IPageDocumentSearchService SearchReturning(params PageDocument[] documents)
        {
            var search = new Mock<IPageDocumentSearchService>();
            search
                .Setup(x => x.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new PageDocumentSearchResult { TotalCount = documents.Length, Results = documents });
            return search.Object;
        }

        private static Store CreateStore(bool pagesEnabled = true)
        {
            return new Store
            {
                Id = "Store",
                DefaultLanguage = "en-US",
                Languages = ["en-US", "nl-NL"],
                Settings =
                [
                    new ObjectSettingEntry { Name = Pages.Core.ModuleConstants.Settings.General.Enable.Name, Value = pagesEnabled },
                ],
            };
        }

        private static GetPageDocumentAlternatesQueryHandler CreateHandler(IPageDocumentSearchService search)
        {
            return CreateHandler(search, CreateStore());
        }

        private static GetPageDocumentAlternatesQueryHandler CreateHandler(IPageDocumentSearchService search, Store store)
        {
            var dependency = new Mock<IOptionalDependency<IPageDocumentSearchService>>();
            dependency.Setup(x => x.HasValue).Returns(true);
            dependency.Setup(x => x.Value).Returns(search);

            var userManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(x => x.Users).Returns(Enumerable.Empty<ApplicationUser>().AsQueryable());

            var storeService = new Mock<IStoreService>();
            storeService
                .Setup(x => x.GetAsync(It.IsAny<IList<string>>(), It.IsAny<string>(), It.IsAny<bool>()))
                .ReturnsAsync(store == null ? [] : [store]);

            return new GetPageDocumentAlternatesQueryHandler(dependency.Object, () => userManager.Object, Mock.Of<IMemberService>(), storeService.Object);
        }
    }
}
