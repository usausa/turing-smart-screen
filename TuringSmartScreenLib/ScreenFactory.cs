namespace TuringSmartScreenLib;

public static class ScreenFactory
{
    public static IScreen Create(ScreenType type, string name)
    {
        if (type == ScreenType.RevisionB)
        {
            var screen = new TuringSmartScreenRevisionB(name);
            screen.Open();
            return (screen.Version & 0x10) != 0
                ? new ScreenWrapperRevisionB1(screen)
                : new ScreenWrapperRevisionB0(screen);
        }

        if (type == ScreenType.RevisionA)
        {
            var screen = new TuringSmartScreenRevisionA(name);
            screen.Open();
            return new ScreenWrapperRevisionA(screen);
        }

        // TODO replace
        if (type == ScreenType.RevisionC)
        {
            var screen = new TuringSmartScreenRevisionC(name, true);
            screen.Open();
            return new ScreenWrapperC(screen);
        }

        throw new NotSupportedException("Unsupported type.");
    }
}
