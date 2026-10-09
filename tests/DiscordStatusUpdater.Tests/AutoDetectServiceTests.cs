// File: AutoDetectServiceTests.cs
// Description: Unit tests for AutoDetectService debouncing and for PresenceBuilder, using a fake process scanner.
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using DiscordStatusUpdater.Models;
using DiscordStatusUpdater.Services;
using Xunit;

namespace DiscordStatusUpdater.Tests;

public class AutoDetectServiceTests
{
    private static readonly DetectionRule CodeRule = new() { Id = "code", Name = "Code", ProcessNames = "code" };
    private static readonly DetectionRule VsRule = new() { Id = "vs", Name = "VS", ProcessNames = "devenv" };

    /// <summary>Builds a service whose "running processes" are whatever the test puts in <paramref name="running"/>.</summary>
    private static (AutoDetectService Svc, List<RunningProcess> Running, List<DetectionMatch?> Changes, List<DetectionMatch> Refreshes)
        Create(params DetectionRule[] rules)
    {
        var running = new List<RunningProcess>();
        var svc = new AutoDetectService(() => rules, () => false, wanted => running.ToList());
        var changes = new List<DetectionMatch?>();
        var refreshes = new List<DetectionMatch>();
        svc.DetectionChanged += (_, m) => changes.Add(m);
        svc.DetectionRefreshed += (_, m) => refreshes.Add(m);
        return (svc, running, changes, refreshes);
    }

    private static RunningProcess Proc(string name, int pid, string title = "") =>
        new(name, pid, title, DateTime.UtcNow, false);

    [Fact]
    public void NewApp_IsReportedOnlyAfterTwoConsecutiveScans()
    {
        var (svc, running, changes, _) = Create(CodeRule);
        running.Add(Proc("Code", 1));

        svc.ScanOnce();
        Assert.Empty(changes);          // first sighting: could be a flicker
        svc.ScanOnce();
        Assert.Single(changes);         // confirmed
        Assert.Equal("code", changes[0]!.Rule.Id);
    }

    [Fact]
    public void BriefAppAppearance_IsIgnored()
    {
        var (svc, running, changes, _) = Create(CodeRule);

        running.Add(Proc("Code", 1));
        svc.ScanOnce();                 // seen once...
        running.Clear();
        svc.ScanOnce();                 // ...gone again: still idle, nothing to report
        svc.ScanOnce();

        Assert.Empty(changes);
    }

    [Fact]
    public void StableApp_IsNotReportedRepeatedly()
    {
        var (svc, running, changes, _) = Create(CodeRule);
        running.Add(Proc("Code", 1));

        for (var i = 0; i < 6; i++) svc.ScanOnce();

        Assert.Single(changes);
    }

    [Fact]
    public void AppClosing_ReportsNullAfterTwoScans()
    {
        var (svc, running, changes, _) = Create(CodeRule);
        running.Add(Proc("Code", 1));
        svc.ScanOnce(); svc.ScanOnce();

        running.Clear();
        svc.ScanOnce();
        Assert.Single(changes);         // not yet
        svc.ScanOnce();

        Assert.Equal(2, changes.Count);
        Assert.Null(changes[1]);
    }

    [Fact]
    public void SwitchingApps_ReportsNewWinner()
    {
        var (svc, running, changes, _) = Create(CodeRule, VsRule);
        running.Add(Proc("Code", 1));
        svc.ScanOnce(); svc.ScanOnce();

        running.Clear();
        running.Add(Proc("devenv", 2));
        svc.ScanOnce(); svc.ScanOnce();

        Assert.Equal(new[] { "code", "vs" }, changes.Select(c => c!.Rule.Id));
    }

    [Fact]
    public void TitleChange_OfSameApp_RaisesRefreshImmediately()
    {
        var (svc, running, changes, refreshes) = Create(CodeRule);
        running.Add(Proc("Code", 1, "a.cs - proj - Visual Studio Code"));
        svc.ScanOnce(); svc.ScanOnce();

        running[0] = Proc("Code", 1, "b.cs - proj - Visual Studio Code");
        svc.ScanOnce();

        Assert.Single(changes);
        Assert.Single(refreshes);
        Assert.Contains("b.cs", refreshes[0].Process.WindowTitle);
    }

    [Fact]
    public void Stop_ResetsState_SoNextStartReportsAgain()
    {
        var (svc, running, changes, _) = Create(CodeRule);
        running.Add(Proc("Code", 1));
        svc.ScanOnce(); svc.ScanOnce();

        svc.Stop();
        svc.ScanOnce(); svc.ScanOnce();

        Assert.Equal(2, changes.Count);
    }
}

public class PresenceBuilderTests
{
    private static ActivityPreset Preset() => new()
    {
        Name = "p",
        Details = "[Editing {file}]",
        State = "[in {workspace}]",
        LargeImageKey = "vscode",
        LargeImageText = "{app}",
        ShowElapsedTime = true,
        Button1Label = "Site",
        Button1Url = "https://example.com",
        Button2Label = "Bad",
        Button2Url = "javascript:alert(1)"
    };

    [Fact]
    public void Templates_AreRenderedIntoPresence()
    {
        var p = PresenceBuilder.Build(Preset(), TemplateContext.Sample);
        Assert.Equal("Editing Program.cs", p.Details);
        Assert.Equal("in my-project", p.State);
        Assert.Equal("Visual Studio Code", p.Assets.LargeImageText);
        Assert.Equal("vscode", p.Assets.LargeImageKey);
    }

    [Fact]
    public void ElapsedTimer_UsesProcessStartTime()
    {
        var start = new DateTime(2026, 1, 2, 3, 4, 5, DateTimeKind.Utc);
        var p = PresenceBuilder.Build(Preset(), TemplateContext.Empty, start);
        Assert.Equal(start, p.Timestamps.Start);
    }

    [Fact]
    public void ElapsedTimer_OmittedWhenPresetHidesIt()
    {
        var preset = Preset();
        preset.ShowElapsedTime = false;
        Assert.Null(PresenceBuilder.Build(preset, TemplateContext.Empty).Timestamps);
    }

    [Fact]
    public void OnlyHttpButtons_AreKept()
    {
        var p = PresenceBuilder.Build(Preset(), TemplateContext.Empty);
        Assert.Single(p.Buttons);
        Assert.Equal("Site", p.Buttons[0].Label);
    }

    [Fact]
    public void EmptyTemplateValues_LeaveFieldsNull()
    {
        var p = PresenceBuilder.Build(Preset(), TemplateContext.Empty);
        Assert.Null(p.Details);
        Assert.Null(p.State);
    }
}
