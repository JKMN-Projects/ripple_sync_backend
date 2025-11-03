namespace RippleSync.Infrastructure.JukmanORM.Enums;

/// <summary>
/// How the ORM should behave when names aren't found
/// </summary>
public enum ParameterNameBehavior
{
    FailOnNotFound,
    NullOnNotFound,
    DefaultOnNotFound
}

