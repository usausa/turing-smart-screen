namespace TuringSmartScreenTool;

public interface IScreenResolver
{
    IScreen Resolve(string revision, string port);
}

public sealed class ScreenResolver : IScreenResolver
{
    public IScreen Resolve(string revision, string port)
    {
        if ((!Enum.TryParse<ScreenType>(revision, true, out var type) && !Enum.TryParse("Revision" + revision, true, out type)) ||
            !Enum.IsDefined(type))
        {
            throw new ArgumentException($"Unknown revision. revision=[{revision}], available=[{String.Join(", ", Enum.GetNames<ScreenType>())}]", nameof(revision));
        }

        return ScreenFactory.Create(type, port);
    }
}
