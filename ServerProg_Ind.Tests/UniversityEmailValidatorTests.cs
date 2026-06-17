using ServerProg_Ind.Application;

namespace ServerProg_Ind.Tests;

public sealed class UniversityEmailValidatorTests
{
    [Theory]
    [InlineData("test@sfedu.com")]
    [InlineData("test@sfedu.ru")]
    [InlineData("student@mail.sfedu.ru")]
    [InlineData("student.name+tag@students.sfedu.org")]
    [InlineData("STUDENT@SFEDU.TEST")]
    public void IsAllowed_ReturnsTrue_ForSfeduAddresses(string email)
    {
        Assert.True(UniversityEmailValidator.IsAllowed(email));
    }

    [Theory]
    [InlineData("student@gmail.com")]
    [InlineData("student@mit.edu")]
    [InlineData("student@notsfedu.test")]
    [InlineData("test@sfedu")]
    [InlineData("test@sfedu.")]
    [InlineData("test@.sfedu.ru")]
    [InlineData("test@sfedu..ru")]
    [InlineData("test@-sfedu.ru")]
    [InlineData("test@sfedu-.ru")]
    [InlineData(".test@sfedu.com")]
    [InlineData("test.@sfedu.com")]
    [InlineData("te..st@sfedu.com")]
    [InlineData("test@@sfedu.com")]
    [InlineData("test @sfedu.com")]
    [InlineData("student.test")]
    [InlineData("Student Name <student@sfedu.test>")]
    public void IsAllowed_ReturnsFalse_ForNonSfeduOrInvalidAddresses(string email)
    {
        Assert.False(UniversityEmailValidator.IsAllowed(email));
    }
}
