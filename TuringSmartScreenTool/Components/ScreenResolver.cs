namespace TuringSmartScreenTool.Components;

public interface IScreenResolver
{
    IScreen Resolve(string revision, string port);
}

public sealed class ScreenResolver : IScreenResolver
{
    public IScreen Resolve(string revision, string port)
    {
        var screenType = Enum.TryParse<ScreenType>(revision, true, out var type)
            ? type
            : Enum.TryParse("Revision" + revision, true, out type)
                ? type
                : ScreenType.RevisionC;

        return ScreenFactory.Create(screenType, port);
    }
}
