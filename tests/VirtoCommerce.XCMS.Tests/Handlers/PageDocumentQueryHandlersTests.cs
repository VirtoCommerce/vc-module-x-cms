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
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Data.Queries;
using Xunit;

namespace VirtoCommerce.XCMS.Tests.Handlers
{
    public class PageDocumentQueryHandlersTests
    {
        [Theory]
        [InlineData(null)]
        [InlineData("org")]
        public async Task GetPageDocuments_FiltersByTheVisitorOrganization(string organizationId)
        {
            PageDocumentSearchCriteria searched = null;
            var handler = new GetPageDocumentQueryHandler(SearchService(x => searched = x), UserManagerFactory(), Mock.Of<IMemberService>());

            await handler.Handle(new GetPageDocumentsQuery { StoreId = "store", OrganizationId = organizationId }, CancellationToken.None);

            searched.FilterByOrganization.Should().BeTrue();
            searched.OrganizationId.Should().Be(organizationId);
        }

        [Theory]
        [InlineData(null)]
        [InlineData("org")]
        public async Task GetSinglePageDocument_FiltersByTheVisitorOrganization(string organizationId)
        {
            PageDocumentSearchCriteria searched = null;
            var handler = new GetSinglePageDocumentQueryHandler(SearchService(x => searched = x), UserManagerFactory(), Mock.Of<IMemberService>());

            await handler.Handle(new GetSinglePageDocumentQuery { Id = "page", OrganizationId = organizationId }, CancellationToken.None);

            searched.FilterByOrganization.Should().BeTrue();
            searched.OrganizationId.Should().Be(organizationId);
            searched.ObjectIds.Should().Equal("page");
        }

        [Fact]
        public async Task GetPageDocuments_ReturnsPublishedPagesOnly()
        {
            PageDocumentSearchCriteria searched = null;
            var handler = new GetPageDocumentQueryHandler(SearchService(x => searched = x), UserManagerFactory(), Mock.Of<IMemberService>());

            await handler.Handle(new GetPageDocumentsQuery { StoreId = "store" }, CancellationToken.None);

            searched.Status.Should().Be(PageDocumentStatus.Published);
        }

        [Fact]
        public async Task GetSinglePageDocument_ReturnsPublishedPagesOnly()
        {
            PageDocumentSearchCriteria searched = null;
            var handler = new GetSinglePageDocumentQueryHandler(SearchService(x => searched = x), UserManagerFactory(), Mock.Of<IMemberService>());

            await handler.Handle(new GetSinglePageDocumentQuery { Id = "page" }, CancellationToken.None);

            searched.Status.Should().Be(PageDocumentStatus.Published);
        }

        private static IOptionalDependency<IPageDocumentSearchService> SearchService(System.Action<PageDocumentSearchCriteria> onSearch)
        {
            var searchService = new Mock<IPageDocumentSearchService>();
            searchService
                .Setup(x => x.SearchAsync(It.IsAny<PageDocumentSearchCriteria>(), It.IsAny<bool>()))
                .Callback((PageDocumentSearchCriteria criteria, bool _) => onSearch(criteria))
                .ReturnsAsync(new PageDocumentSearchResult { Results = [] });

            var dependency = new Mock<IOptionalDependency<IPageDocumentSearchService>>();
            dependency.Setup(x => x.HasValue).Returns(true);
            dependency.Setup(x => x.Value).Returns(searchService.Object);
            return dependency.Object;
        }

        private static System.Func<UserManager<ApplicationUser>> UserManagerFactory()
        {
            var userManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(x => x.Users).Returns(new List<ApplicationUser>().AsQueryable());
            return () => userManager.Object;
        }
    }
}
