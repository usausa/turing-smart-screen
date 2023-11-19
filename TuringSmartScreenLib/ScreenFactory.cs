namespace TuringSmartScreenLib;

public static class ScreenFactory
{
    public static IScreen Create(ScreenType type, string name, int? width = null, int? height = null)
    {
        if (type == ScreenType.RevisionB)
        {
            var screen = new TuringSmartScreenRevisionB(name);
            screen.Open();
            return (screen.Version & 0x10) != 0
                ? new ScreenWrapperRevisionB1(screen, width ?? 320, height ?? 480)
                : new ScreenWrapperRevisionB0(screen, width ?? 320, height ?? 480);
        }

        if (type == ScreenType.RevisionA)
        {
            var screen = new TuringSmartScreenRevisionA(name);
            screen.Open();
            return new ScreenWrapperRevisionA(screen, width ?? 320, height ?? 480);
        }

        if (type == ScreenType.RevisionC)
        {
            var screen = new TuringSmartScreenRevisionC(name, true);
            screen.Open();
            return new ScreenWrapper5Inch(screen, width ?? 800, height ?? 480);
        }

        throw new NotSupportedException("Unsupported type.");
    }
}
