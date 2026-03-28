using BaseLib.BaseLibScenes;
using Godot;

namespace BaseLib.Commands;

/// <summary>
/// Sizes and positions the log window relative to the game window so ultrawide / multi-monitor
/// setups do not inherit a full-screen-width two-thirds rectangle.
/// </summary>
internal static class LogWindowPlacement
{
    internal static Vector2I ComputeDefaultSize(Vector2I hostSize)
    {
        if (hostSize.X <= 0 || hostSize.Y <= 0)
            return new Vector2I(800, 600);

        int tw = hostSize.X * 2 / 3;
        int th = hostSize.Y * 2 / 3;

        // Avoid an extremely wide panel on ultrawide / super-ultrawide fullscreen.
        int maxReadableW = Mathf.Clamp((int)(th * 2.35f), 960, 2048);
        tw = Mathf.Min(tw, maxReadableW);

        tw = Mathf.Min(tw, Mathf.Max(320, hostSize.X - 32));
        th = Mathf.Min(th, Mathf.Max(200, hostSize.Y - 32));

        return new Vector2I(tw, th);
    }

    internal static void ApplyHostWindowDefaults(NLogWindow logWindow, Window host)
    {
        logWindow.CurrentScreen = host.CurrentScreen;
        if (host.ContentScaleFactor > 0f)
            logWindow.ContentScaleFactor = host.ContentScaleFactor;

        logWindow.Size = ComputeDefaultSize(host.Size);
    }
}
