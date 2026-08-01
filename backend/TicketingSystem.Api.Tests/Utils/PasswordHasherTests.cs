using FluentAssertions;
using Xunit;

using TicketingSystem.Api.Services;

namespace TicketingSystem.Api.Tests.Utils;

public class PasswordHasherTests
{
    [Fact]
    public void Hash_WithPassword_ReturnsHash()
    {
        var hash = PasswordHasher.Hash("S3cr3t!");

        hash.Should().NotBeNullOrWhiteSpace();
        hash.Should().NotBe("S3cr3t!");
    }

    [Fact]
    public void Hash_WithSamePassword_ReturnsDifferentHashes()
    {
        var hash1 = PasswordHasher.Hash("S3cr3t!");
        var hash2 = PasswordHasher.Hash("S3cr3t!");

        hash1.Should().NotBe(hash2);
    }

    [Fact]
    public void Verify_WithCorrectPassword_ReturnsTrue()
    {
        var hash = PasswordHasher.Hash("Password123");

        PasswordHasher.Verify("Password123", hash).Should().BeTrue();
    }

    [Fact]
    public void Verify_WithIncorrectPassword_ReturnsFalse()
    {
        var hash = PasswordHasher.Hash("Password123");

        PasswordHasher.Verify("WrongPassword", hash).Should().BeFalse();
    }
}
