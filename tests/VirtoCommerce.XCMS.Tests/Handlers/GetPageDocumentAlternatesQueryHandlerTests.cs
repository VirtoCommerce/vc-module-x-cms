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
                new GetPageDocumentAlternatesQuery { StoreId = "Store", TranslationGroupId = "faq" },
                CancellationToken.None);

            // Assert
            result.Select(x => (x.CultureName, x.Permalink)).Should().Equal(("en-US", "/faq"), ("nl-NL", "/veelgestelde-vragen"));
            criteria.TranslationGroupId.Should().Be("faq");
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

        private static GetPageDocumentAlternatesQueryHandler CreateHandler(IPageDocumentSearchService search)
        {
            var dependency = new Mock<IOptionalDependency<IPageDocumentSearchService>>();
            dependency.Setup(x => x.HasValue).Returns(true);
            dependency.Setup(x => x.Value).Returns(search);

            var userManager = new Mock<UserManager<ApplicationUser>>(
                Mock.Of<IUserStore<ApplicationUser>>(), null, null, null, null, null, null, null, null);
            userManager.Setup(x => x.Users).Returns(Enumerable.Empty<ApplicationUser>().AsQueryable());

            return new GetPageDocumentAlternatesQueryHandler(dependency.Object, () => userManager.Object, Mock.Of<IMemberService>());
        }
    }
}
