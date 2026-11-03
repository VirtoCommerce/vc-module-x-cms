using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.PageBuilderModule.Core.Theme;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Data.Queries;
using Xunit;

namespace VirtoCommerce.XCMS.Tests.Handlers
{
    public class GetThemeSnapshotQueryHandlerTests
    {
        [Fact]
        public async Task Handle_FindsTheStoreAsPageContextDoes_AndReturnsItsSnapshot()
        {
            // Arrange
            StoreDomainRequest storeRequest = null;
            var stores = new Mock<IStoreDomainResolverService>();
            stores
                .Setup(x => x.GetStoreAsync(It.IsAny<StoreDomainRequest>()))
                .Callback<StoreDomainRequest>(x => storeRequest = x)
                .ReturnsAsync(new Store { Id = "B2B-store" });
            var snapshots = new Mock<IThemeSnapshotService>();
            snapshots
                .Setup(x => x.GetPublishedAsync("B2B-store", "de", It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ThemeSnapshot("B2B-store", "de-DE", 4, "{\"format\":1}", []));

            // Act
            var result = await CreateHandler(stores.Object, snapshots.Object, Menus()).Handle(
                new GetThemeSnapshotQuery { Domain = "shop.example.com", CultureName = "de" },
                CancellationToken.None);

            // Assert
            storeRequest.Domain.Should().Be("shop.example.com");
            storeRequest.StoreId.Should().BeNull();
            result.StoreId.Should().Be("B2B-store");
            result.CultureName.Should().Be("de-DE");
            result.Publication.Should().Be(4);
            result.Content.Should().Be("{\"format\":1}");
            result.Menus.Should().BeEmpty();
        }

        [Fact]
        public async Task Handle_ReturnsTheMenusTheSnapshotNames_InTheServedLanguage_UnderTheNamesItHolds()
        {
            // Arrange
            var snapshots = new Mock<IThemeSnapshotService>();
            snapshots
                .Setup(x => x.GetPublishedAsync("B2B-store", null, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ThemeSnapshot("B2B-store", "de-DE", 1, "{}", ["main-menu", "gone"]));
            var menus = Menus(
                List("Main-Menu", "de-DE", "Shop"),
                List("main-menu", "en-US", "Store"),
                List("Shop", "de-DE", "Neu"));

            // Act
            var result = await CreateHandler(Store(), snapshots.Object, menus).Handle(new GetThemeSnapshotQuery { StoreId = "B2B-store" }, CancellationToken.None);

            // Assert
            result.Menus.Should().ContainSingle();
            result.Menus[0].Name.Should().Be("main-menu");
            result.Menus[0].Items.Select(x => x.Link.Title).Should().Equal("Shop");
            result.Menus[0].Items[0].ChildItems.Select(x => x.Link.Title).Should().Equal("Neu");
        }

        [Fact]
        public async Task Handle_WithTheNewPageBuilderOff_ReturnsNull()
        {
            // Arrange
            var snapshots = new Mock<IThemeSnapshotService>();
            snapshots
                .Setup(x => x.GetPublishedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((ThemeSnapshot)null);

            // Act
            var result = await CreateHandler(Store(), snapshots.Object, new Mock<IMenuLinkListSearchService>(MockBehavior.Strict).Object)
                .Handle(new GetThemeSnapshotQuery { Domain = "shop.example.com" }, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        [Fact]
        public async Task Handle_WithoutPageBuilder_ReturnsNullWithoutLookingUpTheStore()
        {
            // Arrange
            var dependency = new Mock<IOptionalDependency<IThemeSnapshotService>>();
            dependency.Setup(x => x.HasValue).Returns(false);
            var handler = new GetThemeSnapshotQueryHandler(
                new Mock<IStoreDomainResolverService>(MockBehavior.Strict).Object,
                new Mock<IMenuLinkListSearchService>(MockBehavior.Strict).Object,
                dependency.Object);

            // Act
            var result = await handler.Handle(new GetThemeSnapshotQuery { Domain = "shop.example.com" }, CancellationToken.None);

            // Assert
            result.Should().BeNull();
        }

        private static IStoreDomainResolverService Store()
        {
            var stores = new Mock<IStoreDomainResolverService>();
            stores.Setup(x => x.GetStoreAsync(It.IsAny<StoreDomainRequest>())).ReturnsAsync(new Store { Id = "B2B-store" });
            return stores.Object;
        }

        private static MenuLinkList List(string name, string language, params string[] titles) => new()
        {
            Name = name,
            Language = language,
            StoreId = "B2B-store",
            MenuLinks = titles.Select(x => new MenuLink { Title = x, Url = $"/{x}" }).ToList(),
        };

        private static IMenuLinkListSearchService Menus(params MenuLinkList[] lists)
        {
            var search = new Mock<IMenuLinkListSearchService>();
            search
                .Setup(x => x.SearchAsync(It.IsAny<MenuLinkListSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new MenuLinkListSearchResult { TotalCount = lists.Length, Results = lists.ToList() });
            return search.Object;
        }

        private static GetThemeSnapshotQueryHandler CreateHandler(IStoreDomainResolverService stores, IThemeSnapshotService snapshots, IMenuLinkListSearchService menus)
        {
            var dependency = new Mock<IOptionalDependency<IThemeSnapshotService>>();
            dependency.Setup(x => x.HasValue).Returns(true);
            dependency.Setup(x => x.Value).Returns(snapshots);
            return new GetThemeSnapshotQueryHandler(stores, menus, dependency.Object);
        }
    }
}
