using System.Linq.Expressions;
using System.Reflection;
using AuctionSystem.Domain.Abstractions;
using AuctionSystem.Domain.Auctions;
using AuctionSystem.Domain.Users;

namespace AuctionSystem.UnitTests.Domain;

public class RepositoryInterfacesTests
{
    [Fact]
    public void IRepository_HasExpectedMethods()
    {
        var repoType = typeof(IRepository<,>);
        Assert.True(repoType.IsInterface);

        var methods = repoType.GetMethods();

        Assert.Contains(methods, m => m.Name == "GetByIdAsync" && m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>));
        Assert.Contains(methods, m => m.Name == "ListAsync" && m.ReturnType.IsGenericType && m.ReturnType.GetGenericTypeDefinition() == typeof(Task<>)); // sanity: name checked below

        Assert.Contains(methods, m => m.Name == "AddAsync" && m.ReturnType == typeof(Task));
        Assert.Contains(methods, m => m.Name == "Update" && m.ReturnType == typeof(void));
        Assert.Contains(methods, m => m.Name == "Remove" && m.ReturnType == typeof(void));

        // Stronger checks for parameters by name/signature on open generic methods
        var listAsync = methods.Single(m => m.Name == "ListAsync");
        var listParams = listAsync.GetParameters();
        Assert.Equal(2, listParams.Length);
        Assert.True(listParams[0].ParameterType.IsGenericType);
        Assert.Equal(typeof(Expression<>), listParams[0].ParameterType.GetGenericTypeDefinition());
        Assert.Equal(typeof(CancellationToken), listParams[1].ParameterType);
    }

    [Fact]
    public void IUserRepository_InheritsGenericRepositoryAndAddsGetByEmail()
    {
        var t = typeof(IUserRepository);
        Assert.True(t.IsInterface);

        Assert.Contains(t.GetInterfaces(), i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IRepository<,>) &&
            i.GetGenericArguments()[0] == typeof(User) &&
            i.GetGenericArguments()[1] == typeof(Guid));

        var method = t.GetMethod("GetByEmailAsync", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        Assert.Equal(typeof(Task<>), method!.ReturnType.GetGenericTypeDefinition());
        Assert.Equal(typeof(User), method.ReturnType.GetGenericArguments()[0]);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(string), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }

    [Fact]
    public void IAuctionRepository_InheritsGenericRepositoryAndAddsGetWithBids()
    {
        var t = typeof(IAuctionRepository);
        Assert.True(t.IsInterface);

        Assert.Contains(t.GetInterfaces(), i =>
            i.IsGenericType &&
            i.GetGenericTypeDefinition() == typeof(IRepository<,>) &&
            i.GetGenericArguments()[0] == typeof(Auction) &&
            i.GetGenericArguments()[1] == typeof(Guid));

        var method = t.GetMethod("GetWithBidsByIdAsync", BindingFlags.Public | BindingFlags.Instance);
        Assert.NotNull(method);

        Assert.Equal(typeof(Task<>), method!.ReturnType.GetGenericTypeDefinition());
        Assert.Equal(typeof(Auction), method.ReturnType.GetGenericArguments()[0]);

        var parameters = method.GetParameters();
        Assert.Equal(2, parameters.Length);
        Assert.Equal(typeof(Guid), parameters[0].ParameterType);
        Assert.Equal(typeof(CancellationToken), parameters[1].ParameterType);
    }
}