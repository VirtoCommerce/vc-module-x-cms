using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using VirtoCommerce.ContentModule.Core.Model;
using VirtoCommerce.ContentModule.Core.Services;
using VirtoCommerce.XCMS.Core.Queries;
using VirtoCommerce.XCMS.Data.Queries;
using Xunit;

namespace VirtoCommerce.XCMS.Tests.Handlers
{
    public class GetMenusQueryHandlerTests
    {
        [Theory]
        [MemberData(nameof(Data))]
        public async Task GetMenusQueryHandlerTest(GetMenusQuery request, List<string> expectedMenuNames)
        {
            // Arrange
            var menus = new List<MenuLinkList>
            {
                new MenuLinkList
                {
                    StoreId = "Store",
                    Name = "MainMenu",
                    Language = "en-US",
                },
                new MenuLinkList
                {
                    StoreId = "Store",
                    Name = "SubMenu",
                    Language = "ru-RU",
                }
            };

            var menuServiceMock = new Mock<IMenuLinkListSearchService>();
            menuServiceMock
                .Setup(x => x.SearchAsync(It.IsAny<MenuLinkListSearchCriteria>(), It.IsAny<bool>()))
                .ReturnsAsync(new MenuLinkListSearchResult { Results = menus.Where(x => x.StoreId == request.StoreId).ToList() });

            var handler = new GetMenusQueryHandler(menuServiceMock.Object);

            // Act
            var result = await handler.Handle(request, CancellationToken.None);

            // Assert
            result.Menus.Select(x => x.Name).Should().BeEquivalentTo(expectedMenuNames);
        }

        public static IEnumerable<object[]> Data()
        {
            yield return new object[]
            {
                // request
                new GetMenusQuery { StoreId = "NonExistent", },
                // expected
                new List<string>()
            };
            yield return new object[]
            {
                // request
                new GetMenusQuery { StoreId = "Store", },
                // expected
                new List<string>() { "MainMenu", "SubMenu" }
            };
            yield return new object[]
            {
                // request
                new GetMenusQuery { StoreId = "Store", CultureName = "ru-RU" },
                // expected
                new List<string>() { "SubMenu" }
            };
            yield return new object[]
            {
                // request
                new GetMenusQuery { StoreId = "Store", Keyword = "Sub" },
                // expected
                new List<string>() { "SubMenu" }
            };
        }
    }
}
