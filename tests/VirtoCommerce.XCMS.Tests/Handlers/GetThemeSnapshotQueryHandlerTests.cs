using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.PageBuilderModule.Core.Theme;
using VirtoCommerce.StoreModule.Core.Model;
using VirtoCommerce.Xapi.Core.Models;
using VirtoCommerce.Xapi.Core.Services;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Data.Queries;
using Xunit;

namespace VirtoCommerce.XCMS.Tests.Handlers;

public class GetThemeSnapshotQueryHandlerTests
{
    private const string StoreId = "B2B-store";

    [Fact]
    public async Task Handle_FindsTheStoreByDomain_AndReturnsItsSnapshot()
    {
        // Arrange
        StoreDomainRequest storeRequest = null;
        var stores = new Mock<IStoreDomainResolverService>();
        stores
            .Setup(x => x.GetStoreAsync(It.IsAny<StoreDomainRequest>()))
            .Callback<StoreDomainRequest>(x => storeRequest = x)
            .ReturnsAsync(new Store { Id = StoreId });
        var snapshots = new Mock<IThemeSnapshotService>();
        snapshots
            .Setup(x => x.GetPublishedAsync(StoreId, "de", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ThemeSnapshot(StoreId, "de-DE", 4, "{\"format\":1}", []));

        // Act
        var result = await CreateHandler(stores.Object, snapshots.Object, new Mock<IMenuLinkListSearchService>(MockBehavior.Strict).Object).Handle(
            new GetThemeSnapshotQuery { Domain = "shop.example.com", CultureName = "de" },
            CancellationToken.None);

        // Assert
        storeRequest.Domain.Should().Be("shop.example.com");
        storeRequest.StoreId.Should().BeNull();
        result.StoreId.Should().Be(StoreId);
        result.CultureName.Should().Be("de-DE");
        result.Publication.Should().Be(4);
        result.Content.Should().Be("{\"format\":1}");
        result.Menus.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PassesTheStoreIdToTheStoreLookup()
    {
        // Arrange
        StoreDomainRequest storeRequest = null;
        var stores = new Mock<IStoreDomainResolverService>();
        stores
            .Setup(x => x.GetStoreAsync(It.IsAny<StoreDomainRequest>()))
            .Callback<StoreDomainRequest>(x => storeRequest = x)
            .ReturnsAsync(new Store { Id = StoreId });

        // Act
        await CreateHandler(stores.Object, Snapshots(new ThemeSnapshot(StoreId, "en-US", 1, "{}", [])), Menus())
            .Handle(new GetThemeSnapshotQuery { StoreId = StoreId, Domain = "shop.example.com" }, CancellationToken.None);

        // Assert
        storeRequest.StoreId.Should().Be(StoreId);
        storeRequest.Domain.Should().Be("shop.example.com");
    }

    [Fact]
    public async Task Handle_ReturnsTheStoreMenusTheSnapshotNames_InTheServedLanguage_UnderTheNamesItHolds()
    {
        // Arrange
        var menus = Menus(
            List("Other-store", "main-menu", "de-DE", "Fremd"),
            List(StoreId, "Main-Menu", "de-DE", "Shop"),
            List(StoreId, "main-menu", "en-US", "Store"),
            List(StoreId, "Shop", "de-DE", "Neu"));

        // Act
        var result = await CreateHandler(Store(), Snapshots(new ThemeSnapshot(StoreId, "de-DE", 1, "{}", ["main-menu", "gone"])), menus)
            .Handle(new GetThemeSnapshotQuery { Domain = "shop.example.com" }, CancellationToken.None);

        // Assert
        result.Menus.Should().ContainSingle();
        result.Menus[0].Name.Should().Be("main-menu");
        result.Menus[0].Items.Select(x => x.Link.Title).Should().Equal("Shop");
        result.Menus[0].Items[0].ChildItems.Select(x => x.Link.Title).Should().Equal("Neu");
    }

    [Fact]
    public async Task Handle_WithoutTheStore_ReturnsNull()
    {
        // Arrange
        var stores = new Mock<IStoreDomainResolverService>();
        stores.Setup(x => x.GetStoreAsync(It.IsAny<StoreDomainRequest>())).ReturnsAsync((Store)null);

        // Act
        var result = await CreateHandler(
                stores.Object,
                new Mock<IThemeSnapshotService>(MockBehavior.Strict).Object,
                new Mock<IMenuLinkListSearchService>(MockBehavior.Strict).Object)
            .Handle(new GetThemeSnapshotQuery { Domain = "unknown.example.com" }, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WithTheNewPageBuilderOff_ReturnsNull()
    {
        // Act
        var result = await CreateHandler(Store(), Snapshots(null), new Mock<IMenuLinkListSearchService>(MockBehavior.Strict).Object)
            .Handle(new GetThemeSnapshotQuery { Domain = "shop.example.com" }, CancellationToken.None);

        // Assert
        result.Should().BeNull();
    }

    private static IStoreDomainResolverService Store()
    {
        var stores = new Mock<IStoreDomainResolverService>();
        stores.Setup(x => x.GetStoreAsync(It.IsAny<StoreDomainRequest>())).ReturnsAsync(new Store { Id = StoreId });
        return stores.Object;
    }

    private static IThemeSnapshotService Snapshots(ThemeSnapshot snapshot)
    {
        var snapshots = new Mock<IThemeSnapshotService>();
        snapshots
            .Setup(x => x.GetPublishedAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(snapshot);
        return snapshots.Object;
    }

    private static MenuLinkList List(string storeId, string name, string language, params string[] titles) => new()
    {
        Name = name,
        Language = language,
        StoreId = storeId,
        MenuLinks = titles.Select(x => new MenuLink { Title = x, Url = $"/{x}" }).ToList(),
    };

    private static IMenuLinkListSearchService Menus(params MenuLinkList[] lists)
    {
        var search = new Mock<IMenuLinkListSearchService>();
        search
            .Setup(x => x.SearchAsync(It.IsAny<MenuLinkListSearchCriteria>(), It.IsAny<bool>()))
            .ReturnsAsync((MenuLinkListSearchCriteria criteria, bool _) =>
            {
                var results = lists.Where(x => x.StoreId == criteria.StoreId).ToList();
                return new MenuLinkListSearchResult { TotalCount = results.Count, Results = results };
            });
        return search.Object;
    }

    private static GetThemeSnapshotQueryHandler CreateHandler(IStoreDomainResolverService stores, IThemeSnapshotService snapshots, IMenuLinkListSearchService menus)
    {
        return new GetThemeSnapshotQueryHandler(stores, menus, snapshots);
    }
}
