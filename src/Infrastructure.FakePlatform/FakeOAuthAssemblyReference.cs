using System.Reflection;

namespace Infrastructure.FakePlatform;

public static class FakeOAuthAssemblyReference
{
    public static Assembly Assembly => typeof(FakeOAuthController).Assembly;
}