using System;
using System.Diagnostics;
using System.IO;
using Engine.Graphics;
using Xunit;

namespace Engine.Tests.Graphics;

public class CircleTextureGeneratorTests
{
    [Fact]
    [Trait("Category", "GraphicsLogic")]
    public void MakeFilledCircleRgba_ReturnsExpectedLength()
    {
        const int diameter = 16;

        var bytes = CircleTextureGenerator.MakeFilledCircleRgba(diameter);

        Assert.Equal(diameter * diameter * 4, bytes.Length);
    }

    [Fact]
    [Trait("Category", "GraphicsLogic")]
    public void MakeFilledCircleRgba_HasOpaqueCenterAndTransparentCorner()
    {
        const int diameter = 20;
        var bytes = CircleTextureGenerator.MakeFilledCircleRgba(diameter);

        int centerAlpha = AlphaAt(bytes, diameter, diameter / 2, diameter / 2);
        int cornerAlpha = AlphaAt(bytes, diameter, 0, 0);

        Assert.True(centerAlpha >= 250);
        Assert.True(cornerAlpha <= 5);
    }

    [Fact]
    [Trait("Category", "GraphicsLogic")]
    public void MakeFilledCircleRgba_IsSymmetricAcrossHorizontalAxis()
    {
        const int diameter = 20;
        var bytes = CircleTextureGenerator.MakeFilledCircleRgba(diameter);

        int yTop = 4;
        int yBottom = diameter - 1 - yTop;
        int x = diameter / 2;

        int topAlpha = AlphaAt(bytes, diameter, x, yTop);
        int bottomAlpha = AlphaAt(bytes, diameter, x, yBottom);

        Assert.InRange(Math.Abs(topAlpha - bottomAlpha), 0, 1);
    }

    [Fact]
    [Trait("Category", "GraphicsIntegration")]
    public void GraphicsIntegration_Smoke_RunsEngineExeForShortPeriod()
    {
        // Opt-in guard: set RUN_GRAPHICS_INTEGRATION_TESTS=1 to enable this test locally/CI where a GPU is available.
        if (Environment.GetEnvironmentVariable("RUN_GRAPHICS_INTEGRATION_TESTS") != "1")
            return; // opt-in guard: not enabled, treat as no-op (test passes)

        // Locate the built Engine executable (Debug output). If missing, skip the test.
        var exePath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "Engine", "bin", "Debug", "net8.0", "win-x64", "Engine.exe"));
        if (!File.Exists(exePath))
            return; // Engine not built; opt-out of integration run

        var psi = new ProcessStartInfo(exePath)
        {
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            WorkingDirectory = Path.GetDirectoryName(exePath)
        };

        using var proc = Process.Start(psi);
        Assert.NotNull(proc);

        try
        {
            // Give the app a short moment to initialize — it should remain running if successful.
            bool exited = proc.WaitForExit(1000);
            Assert.False(exited, "Engine exited prematurely during startup.");
        }
        finally
        {
            if (!proc.HasExited)
            {
                proc.Kill(true);
                proc.WaitForExit(2000);
            }
        }
    }

    private static int AlphaAt(byte[] bytes, int diameter, int x, int y)
    {
        int i = (y * diameter + x) * 4;
        return bytes[i + 3];
    }
}
