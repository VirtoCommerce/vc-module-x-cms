using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.PageBuilderModule.Core.Services;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Schemas;
using Xunit;

namespace VirtoCommerce.XCMS.Tests.Schemas;

public class BuilderPageItemTypeTests
{
    private const string PageId = "draft-page";
    private const string RawContent = "{\"content\":[{\"id\":\"placement\",\"type\":\"componentRef\",\"componentRef\":\"shared\"}]}";
    private const string ResolvedContent = "{\"content\":[{\"id\":\"placement_title\",\"type\":\"text\",\"text\":\"Shared title\"}]}";

    [Fact]
    public async Task Content_GraphQLFieldUsesRegisteredProvider()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IOptionalDependency<IGroupedPageService>>(CreateGroupedPages());
        services.AddSingleton(CreateProvider("PageBuilder", [new PageDocument { Id = PageId, Content = ResolvedContent }]).Object);
        using var serviceProvider = services.BuildServiceProvider();
        var schema = ActivatorUtilities.CreateInstance<BuilderPageItemType>(serviceProvider);
        var field = schema.Fields.Single(x => x.Name == nameof(BuilderPageItem.Content));

        var result = await field.Resolver.ResolveAsync(new ResolveFieldContext
        {
            Source = new BuilderPageItem { PageId = PageId },
        });

        Assert.Equal(ResolvedContent, result);
    }

    [Theory]
    [InlineData(ResolvedContent)]
    [InlineData("{\"content\":[{\"type\":\"text\",\"text\":\"Ordinary section\"}]}")]
    public async Task Content_UsesPageBuilderDocumentInsteadOfRawAuthoringContent(string content)
    {
        var groupedPages = CreateGroupedPages();
        var provider = CreateProvider("PageBuilder", [new PageDocument { Id = PageId, Content = content }]);
        var otherProvider = new Mock<IPageContentProvider>(MockBehavior.Strict);
        otherProvider.SetupGet(x => x.ProviderName).Returns("Other");
        var schema = new TestBuilderPageItemType(groupedPages, [otherProvider.Object, provider.Object]);

        var result = await schema.ResolveContentAsync(new BuilderPageItem { PageId = PageId });

        Assert.Equal(content, result);
        Assert.DoesNotContain("componentRef", result);
        groupedPages.Value.Verify(x => x.LoadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Content_WhenProviderSkipsInvalidPage_DoesNotExposeRawReferences()
    {
        var groupedPages = CreateGroupedPages();
        var provider = CreateProvider("PageBuilder", []);
        var schema = new TestBuilderPageItemType(groupedPages, [provider.Object]);

        var result = await schema.ResolveContentAsync(new BuilderPageItem { PageId = PageId });

        Assert.Null(result);
        groupedPages.Value.Verify(x => x.LoadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Content_DoesNotReturnAnotherPageFromProvider()
    {
        var provider = CreateProvider("PageBuilder", [new PageDocument { Id = "other-page", Content = "Other content" }]);
        var schema = new TestBuilderPageItemType(CreateGroupedPages(), [provider.Object]);

        Assert.Null(await schema.ResolveContentAsync(new BuilderPageItem { PageId = PageId }));
    }

    [Fact]
    public async Task Content_RefreshReadsCurrentComponentContent()
    {
        var provider = CreateProvider("PageBuilder", []);
        provider.SetupSequence(x => x.GetByIdsAsync(It.Is<IList<string>>(ids => ids.SequenceEqual(new[] { PageId }))))
            .ReturnsAsync(new List<PageDocument> { new() { Id = PageId, Content = ResolvedContent } })
            .ReturnsAsync(new List<PageDocument> { new() { Id = PageId, Content = "Updated shared title" } });
        var schema = new TestBuilderPageItemType(CreateGroupedPages(), [provider.Object]);

        Assert.Equal(ResolvedContent, await schema.ResolveContentAsync(new BuilderPageItem { PageId = PageId }));
        Assert.Equal("Updated shared title", await schema.ResolveContentAsync(new BuilderPageItem { PageId = PageId }));
    }

    [Fact]
    public async Task Content_StaticContentIsPreserved()
    {
        var provider = new Mock<IPageContentProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns("PageBuilder");
        var schema = new TestBuilderPageItemType(CreateGroupedPages(), [provider.Object]);

        Assert.Equal("Static content", await schema.ResolveContentAsync(new BuilderPageItem
        {
            PageId = PageId,
            Content = "Static content",
        }));
    }

    [Fact]
    public async Task Content_WithoutContentProvider_PreservesLegacyPageBuilderSupport()
    {
        var schema = new TestBuilderPageItemType(CreateGroupedPages(), []);

        Assert.Equal(RawContent, await schema.ResolveContentAsync(new BuilderPageItem { PageId = PageId }));
    }

    [Fact]
    public async Task Content_WithoutPageId_DoesNotRequestDocument()
    {
        var provider = new Mock<IPageContentProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns("PageBuilder");
        var schema = new TestBuilderPageItemType(CreateGroupedPages(), [provider.Object]);

        Assert.Null(await schema.ResolveContentAsync(new BuilderPageItem()));
    }

    [Fact]
    public async Task Content_WithoutPageBuilder_ReturnsExistingContent()
    {
        var groupedPages = new Mock<IOptionalDependency<IGroupedPageService>>(MockBehavior.Strict);
        groupedPages.SetupGet(x => x.HasValue).Returns(false);
        var schema = new TestBuilderPageItemType(groupedPages.Object, []);

        Assert.Equal("Static content", await schema.ResolveContentAsync(new BuilderPageItem { Content = "Static content" }));
        Assert.Null(await schema.ResolveContentAsync(new BuilderPageItem { PageId = PageId }));
    }

    private static Mock<IPageContentProvider> CreateProvider(string name, IList<PageDocument> documents)
    {
        var provider = new Mock<IPageContentProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns(name);
        provider.Setup(x => x.GetByIdsAsync(It.Is<IList<string>>(ids => ids.SequenceEqual(new[] { PageId }))))
            .ReturnsAsync(documents);
        return provider;
    }

    private static GroupedPagesDependency CreateGroupedPages()
    {
        var service = new Mock<IGroupedPageService>(MockBehavior.Strict);
        service.Setup(x => x.LoadContent(PageId, It.IsAny<CancellationToken>())).ReturnsAsync(RawContent);
        return new GroupedPagesDependency(service);
    }

    private sealed class GroupedPagesDependency(Mock<IGroupedPageService> service) : IOptionalDependency<IGroupedPageService>
    {
        public bool HasValue => true;
        public Mock<IGroupedPageService> Value => service;
        IGroupedPageService IOptionalDependency<IGroupedPageService>.Value => service.Object;
    }

    private sealed class TestBuilderPageItemType(
        IOptionalDependency<IGroupedPageService> groupedPages,
        IEnumerable<IPageContentProvider> providers) : BuilderPageItemType(groupedPages, providers)
    {
        public Task<string> ResolveContentAsync(BuilderPageItem source)
        {
            return LoadContent(new ResolveFieldContext<BuilderPageItem> { Source = source });
        }
    }
}
