using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using SmartExpense.Api.Authentication;

namespace SmartExpense.Tests.Api.Authentication;

public sealed class HttpContextCurrentUserTests
{
    [Fact]
    public void UserId_WithAuthenticatedPrincipalAndValidSubject_ReturnsGuid()
    {
        var expectedUserId = Guid.NewGuid();
        var currentUser = CreateCurrentUser(
            new ClaimsIdentity(
                [new Claim("sub", expectedUserId.ToString())],
                authenticationType: "Bearer"));

        Assert.Equal(expectedUserId, currentUser.UserId);
    }

    [Fact]
    public void UserId_WithoutHttpContext_ReturnsNull()
    {
        var currentUser = new HttpContextCurrentUser(new HttpContextAccessor());

        Assert.Null(currentUser.UserId);
    }

    [Fact]
    public void UserId_WithUnauthenticatedPrincipal_ReturnsNull()
    {
        var currentUser = CreateCurrentUser(
            new ClaimsIdentity(
                [new Claim("sub", Guid.NewGuid().ToString())]));

        Assert.Null(currentUser.UserId);
    }

    [Fact]
    public void UserId_WithoutSubjectClaim_ReturnsNull()
    {
        var currentUser = CreateCurrentUser(
            new ClaimsIdentity(authenticationType: "Bearer"));

        Assert.Null(currentUser.UserId);
    }

    [Fact]
    public void UserId_WithMalformedSubjectClaim_ReturnsNull()
    {
        var currentUser = CreateCurrentUser(
            new ClaimsIdentity(
                [new Claim("sub", "not-a-guid")],
                authenticationType: "Bearer"));

        Assert.Null(currentUser.UserId);
    }

    private static HttpContextCurrentUser CreateCurrentUser(
        ClaimsIdentity identity)
    {
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(identity)
        };

        return new HttpContextCurrentUser(new HttpContextAccessor
        {
            HttpContext = context
        });
    }
}
