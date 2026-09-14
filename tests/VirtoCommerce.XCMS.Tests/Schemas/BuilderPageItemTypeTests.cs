using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using VirtoCommerce.PageBuilderModule.Core.Services;
using VirtoCommerce.Pages.Core.ContentProviders;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Modularity;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Schemas;
using Xunit;

namespace VirtoCommerce.XCMS.Tests.Schemas;

public class BuilderPageItemTypeTests : IDisposable
{
    private const string PageId = "draft-page";
    private const string RawContent = "{\"content\":[{\"id\":\"placement\",\"type\":\"componentRef\",\"componentRef\":\"shared\"}]}";
    private const string ResolvedContent = "{\"content\":[{\"id\":\"placement_title\",\"type\":\"text\",\"text\":\"Shared title\"}]}";

    private readonly List<ServiceProvider> _serviceProviders = [];

    [Fact]
    public async Task Content_GraphQLFieldUsesRegisteredProvider()
    {
        var schema = new BuilderPageItemType(CreateGroupedPages());
        var field = schema.Fields.Single(x => x.Name == nameof(BuilderPageItem.Content));
        var provider = CreateProvider("PageBuilder", [new PageDocument { Id = PageId, Content = ResolvedContent }]);

        var result = await field.Resolver.ResolveAsync(new ResolveFieldContext
        {
            Source = new BuilderPageItem { PageId = PageId },
            RequestServices = CreateRequestServices(provider.Object),
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
        var schema = new TestBuilderPageItemType(groupedPages);

        var result = await ResolveContentAsync(
            schema,
            new BuilderPageItem { PageId = PageId },
            otherProvider.Object,
            provider.Object);

        Assert.Equal(content, result);
        groupedPages.Value.Verify(x => x.LoadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData("PageBuilder")]
    [InlineData("pagebuilder")]
    public async Task Content_MatchesProviderNameIgnoringCase(string providerName)
    {
        var provider = CreateProvider(providerName, [new PageDocument { Id = PageId, Content = ResolvedContent }]);
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        var result = await ResolveContentAsync(schema, new BuilderPageItem { PageId = PageId }, provider.Object);

        Assert.Equal(ResolvedContent, result);
    }

    [Fact]
    public async Task Content_WhenProviderSkipsInvalidPage_DoesNotExposeRawReferences()
    {
        var groupedPages = CreateGroupedPages();
        var provider = CreateProvider("PageBuilder", []);
        var schema = new TestBuilderPageItemType(groupedPages);

        var result = await ResolveContentAsync(schema, new BuilderPageItem { PageId = PageId }, provider.Object);

        Assert.Null(result);
        groupedPages.Value.Verify(x => x.LoadContent(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Content_WhenProviderSkipsInvalidPage_LogsWarning()
    {
        var provider = CreateProvider("PageBuilder", []);
        var logger = new Mock<ILogger<BuilderPageItemType>>();
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        var context = new ResolveFieldContext<BuilderPageItem>
        {
            Source = new BuilderPageItem { PageId = PageId },
            RequestServices = CreateRequestServices([provider.Object], logger.Object),
        };

        Assert.Null(await schema.ResolveContentAsync(context));

        logger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((state, _) => state.ToString().Contains(PageId)),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task Content_DoesNotReturnAnotherPageFromProvider()
    {
        var provider = CreateProvider("PageBuilder", [new PageDocument { Id = "other-page", Content = "Other content" }]);
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        Assert.Null(await ResolveContentAsync(schema, new BuilderPageItem { PageId = PageId }, provider.Object));
    }

    [Fact]
    public async Task Content_RefreshReadsCurrentComponentContent()
    {
        var provider = CreateProvider("PageBuilder", []);
        provider.SetupSequence(x => x.GetByIdsAsync(It.Is<IList<string>>(ids => ids.SequenceEqual(new[] { PageId }))))
            .ReturnsAsync(new List<PageDocument> { new() { Id = PageId, Content = ResolvedContent } })
            .ReturnsAsync(new List<PageDocument> { new() { Id = PageId, Content = "Updated shared title" } });
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        Assert.Equal(ResolvedContent, await ResolveContentAsync(schema, new BuilderPageItem { PageId = PageId }, provider.Object));
        Assert.Equal("Updated shared title", await ResolveContentAsync(schema, new BuilderPageItem { PageId = PageId }, provider.Object));
    }

    [Fact]
    public async Task Content_StaticContentIsPreserved()
    {
        var provider = new Mock<IPageContentProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns("PageBuilder");
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        var source = new BuilderPageItem
        {
            PageId = PageId,
            Content = "Static content",
        };

        Assert.Equal("Static content", await ResolveContentAsync(schema, source, provider.Object));
    }

    [Fact]
    public async Task Content_WithoutContentProvider_PreservesLegacyPageBuilderSupport()
    {
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        Assert.Equal(RawContent, await ResolveContentAsync(schema, new BuilderPageItem { PageId = PageId }));
    }

    [Fact]
    public async Task Content_WithoutRequestScope_PreservesLegacyPageBuilderSupport()
    {
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        var result = await schema.ResolveContentAsync(new ResolveFieldContext<BuilderPageItem>
        {
            Source = new BuilderPageItem { PageId = PageId },
        });

        Assert.Equal(RawContent, result);
    }

    [Fact]
    public async Task Content_WithoutPageId_DoesNotRequestDocument()
    {
        var provider = new Mock<IPageContentProvider>(MockBehavior.Strict);
        provider.SetupGet(x => x.ProviderName).Returns("PageBuilder");
        var schema = new TestBuilderPageItemType(CreateGroupedPages());

        Assert.Null(await ResolveContentAsync(schema, new BuilderPageItem(), provider.Object));
    }

    [Fact]
    public async Task Content_WithoutPageBuilder_ReturnsExistingContent()
    {
        var groupedPages = new Mock<IOptionalDependency<IGroupedPageService>>(MockBehavior.Strict);
        groupedPages.SetupGet(x => x.HasValue).Returns(false);
        var schema = new TestBuilderPageItemType(groupedPages.Object);

        Assert.Equal("Static content", await ResolveContentAsync(schema, new BuilderPageItem { Content = "Static content" }));
        Assert.Null(await ResolveContentAsync(schema, new BuilderPageItem { PageId = PageId }));
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private Task<string> ResolveContentAsync(
        TestBuilderPageItemType schema,
        BuilderPageItem source,
        params IPageContentProvider[] providers)
    {
        return schema.ResolveContentAsync(new ResolveFieldContext<BuilderPageItem>
        {
            Source = source,
            RequestServices = CreateRequestServices(providers),
        });
    }

    private IServiceProvider CreateRequestServices(params IPageContentProvider[] providers)
    {
        return CreateRequestServices(providers, logger: null);
    }

    private IServiceProvider CreateRequestServices(IPageContentProvider[] providers, ILogger<BuilderPageItemType> logger)
    {
        var services = new ServiceCollection();

        foreach (var provider in providers)
        {
            services.AddSingleton(provider);
        }

        if (logger != null)
        {
            services.AddSingleton(logger);
        }

        var serviceProvider = services.BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);

        return serviceProvider;
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

    private sealed class TestBuilderPageItemType(IOptionalDependency<IGroupedPageService> groupedPages)
        : BuilderPageItemType(groupedPages)
    {
        public Task<string> ResolveContentAsync(IResolveFieldContext<BuilderPageItem> context)
        {
            return LoadContent(context);
        }
    }
}
