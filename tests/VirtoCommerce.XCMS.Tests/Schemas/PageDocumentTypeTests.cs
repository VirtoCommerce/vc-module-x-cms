using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using GraphQL;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using VirtoCommerce.Pages.Core.Models;
using VirtoCommerce.Platform.Core.Security;
using VirtoCommerce.Xapi.Core.Infrastructure;
using VirtoCommerce.XCMS.Core.Models;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Core.Schemas;
using Xunit;

using static VirtoCommerce.CustomerModule.Core.ModuleConstants.Security;

namespace VirtoCommerce.XCMS.Tests.Schemas;

public class PageDocumentTypeTests : IDisposable
{
    private const string UserIdClaimType = ClaimTypes.NameIdentifier;

    private readonly List<ServiceProvider> _serviceProviders = [];

    public PageDocumentTypeTests()
    {
        if (!ClaimsPrincipalExtensions.UserIdClaimTypes.Contains(UserIdClaimType))
        {
            ClaimsPrincipalExtensions.UserIdClaimTypes = [.. ClaimsPrincipalExtensions.UserIdClaimTypes, UserIdClaimType];
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Alternates_WithoutTranslationGroup_ReturnsEmptyWithoutQuery(string translationGroupId)
    {
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        var schema = new TestPageDocumentType();

        var result = await schema.ResolveAlternatesAsync(CreateContext(
            new PageDocument { Id = "page", StoreId = "Store", TranslationGroupId = translationGroupId },
            mediator.Object));

        Assert.Empty(Assert.IsType<IEnumerable<PageDocumentAlternate>>(result, exactMatch: false));
    }

    [Fact]
    public async Task Alternates_SendsQueryForPageStoreAndCurrentUser()
    {
        GetPageDocumentAlternatesQuery query = null;
        IList<PageDocumentAlternate> alternates = [new PageDocumentAlternate { Id = "page", CultureName = "en-US", Permalink = "/faq" }];
        var mediator = new Mock<IMediator>(MockBehavior.Strict);
        mediator
            .Setup(x => x.Send(It.IsAny<GetPageDocumentAlternatesQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IList<PageDocumentAlternate>>, CancellationToken>((request, _) => query = (GetPageDocumentAlternatesQuery)request)
            .ReturnsAsync(alternates);
        var schema = new TestPageDocumentType();

        var result = await schema.ResolveAlternatesAsync(CreateContext(
            new PageDocument { Id = "page", StoreId = "PageStore", TranslationGroupId = "faq" },
            mediator.Object,
            new Claim(UserIdClaimType, "User"),
            new Claim(Claims.OrganizationId, "Organization")));

        Assert.Same(alternates, result);
        Assert.NotNull(query);
        Assert.Equal("PageStore", query.StoreId);
        Assert.Equal("faq", query.TranslationGroupId);
        Assert.Equal("User", query.UserId);
        Assert.Equal("Organization", query.OrganizationId);
    }

    public void Dispose()
    {
        foreach (var serviceProvider in _serviceProviders)
        {
            serviceProvider.Dispose();
        }

        GC.SuppressFinalize(this);
    }

    private ResolveFieldContext<PageDocument> CreateContext(PageDocument source, IMediator mediator, params Claim[] claims)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mediator);

        var serviceProvider = services.BuildServiceProvider();
        _serviceProviders.Add(serviceProvider);

        return new ResolveFieldContext<PageDocument>
        {
            Source = source,
            RequestServices = serviceProvider,
            UserContext = new GraphQLUserContext(new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"))),
        };
    }

    private sealed class TestPageDocumentType : PageDocumentType
    {
        public Task<object> ResolveAlternatesAsync(IResolveFieldContext<PageDocument> context)
        {
            return LoadAlternates(context);
        }
    }
}
