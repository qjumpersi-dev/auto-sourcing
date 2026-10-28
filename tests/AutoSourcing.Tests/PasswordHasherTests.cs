using AutoSourcing.Services.Auth;
using Xunit;

namespace AutoSourcing.Tests;

public class PasswordHasherTests
{
    private readonly Pbkdf2PasswordHasher _sut = new();

    [Fact]
    public void Hash_ThenVerify_ReturnsTrue()
    {
        var hash = _sut.Hash("Str0ng!Password");

        Assert.True(_sut.Verify("Str0ng!Password", hash));
    }

    [Fact]
    public void Verify_WithWrongPassword_ReturnsFalse()
    {
        var hash = _sut.Hash("Str0ng!Password");

        Assert.False(_sut.Verify("wrong-password", hash));
    }

    [Fact]
    public void Hash_IsSalted_SoSamePasswordProducesDifferentHashes()
    {
        var first = _sut.Hash("same-password");
        var second = _sut.Hash("same-password");

        Assert.NotEqual(first, second);
        Assert.True(_sut.Verify("same-password", first));
        Assert.True(_sut.Verify("same-password", second));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("pbkdf2:notanumber:c2FsdA==:a2V5")]
    [InlineData("pbkdf2:100000:not-base64:not-base64")]
    [InlineData("md5:1000:c2FsdA==:a2V5")]
    public void Verify_WithMalformedHash_ReturnsFalse(string hash)
    {
        Assert.False(_sut.Verify("anything", hash));
    }
}
