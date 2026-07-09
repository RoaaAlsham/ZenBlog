using Microsoft.AspNetCore.Identity;
using Moq;
using ZenBlog.Application.Base;
using ZenBlog.Application.Contracts.Identity;
using ZenBlog.Application.Contracts.Persistence;
using ZenBlog.Application.Features.Blogs.Commands;
using ZenBlog.Application.Features.Blogs.Handlers;
using ZenBlog.Domain.Entities;

namespace ZenBlog.Application.Tests.Features.Blogs.Handlers;

public class RemoveBlogCommandHandlerTests
{
    [Fact]
    public async Task Handle_NonOwnerNonAdmin_ReturnsFailureAndDoesNotDelete()
    {
        var repository = new Mock<IRepository<Blog>>(MockBehavior.Strict);
        var unitOfWork = new Mock<IUnitOfWork>(MockBehavior.Strict);
        var currentUser = new Mock<ICurrentUserService>(MockBehavior.Strict);
        var userManager = CreateUserManagerMock();

        var command = new RemoveBlogCommand(Guid.NewGuid());
        var blog = new Blog
        {
            Id = command.Id,
            Title = "Title",
            Description = "Description",
            CategoryId = Guid.NewGuid(),
            UserId = "blog-owner-id"
        };
        var caller = new AppUser
        {
            Id = "different-user-id",
            Email = "caller@example.com",
            UserName = "caller@example.com",
            FirstName = "Caller",
            LastName = "User"
        };

        repository
            .Setup(x => x.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(blog);
        currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        currentUser.SetupGet(x => x.UserId).Returns(caller.Id);
        userManager.Setup(x => x.FindByIdAsync(caller.Id)).ReturnsAsync(caller);
        userManager.Setup(x => x.GetRolesAsync(caller)).ReturnsAsync((IList<string>)new List<string> { "User" });

        var sut = new RemoveBlogCommandHandler(
            repository.Object,
            unitOfWork.Object,
            currentUser.Object,
            userManager.Object);

        var result = await sut.Handle(command, CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(ResultStatus.Forbidden, result.Status);
        Assert.Equal("You are not authorized to delete this blog.", Assert.Single(result.Errors).ErrorMessage);
        repository.Verify(x => x.DeleteAsync(It.IsAny<Blog>()), Times.Never);
        unitOfWork.Verify(x => x.SaveChangesAsync(), Times.Never);
    }

    private static Mock<UserManager<AppUser>> CreateUserManagerMock()
    {
        var store = new Mock<IUserStore<AppUser>>();
        return new Mock<UserManager<AppUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }
}
