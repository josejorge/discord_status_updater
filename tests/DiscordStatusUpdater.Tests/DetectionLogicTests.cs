// File: DetectionLogicTests.cs
// Description: Unit tests for TitleParser, TemplateEngine and RuleMatcher (the pure auto-detect logic).
// Author: Jose-Jorge HERNANDEZ
// Company: Parlee Conseiller, Inc.
// Date: 2026-10-09
// Last edit date: 2026-10-09
// Version: 1.0.0

using DiscordStatusUpdater.Models;
using DiscordStatusUpdater.Services;
using Xunit;

namespace DiscordStatusUpdater.Tests;

public class TitleParserTests
{
    [Fact]
    public void VsCodeTitle_SplitsFileAndWorkspace()
    {
        var p = TitleParser.Parse("main.cs - my-project - Visual Studio Code");
        Assert.Equal("main.cs", p.File);
        Assert.Equal("my-project", p.Workspace);
    }

    [Fact]
    public void SingleSegmentPlusApp_IsWorkspaceOnly()
    {
        var p = TitleParser.Parse("MySolution - Microsoft Visual Studio");
        Assert.Equal("", p.File);
        Assert.Equal("MySolution", p.Workspace);
    }

    [Fact]
    public void UnsavedMarker_IsStripped()
    {
        Assert.Equal("a.py", TitleParser.Parse("● a.py - proj - Visual Studio Code").File);
    }

    [Fact]
    public void HyphenInsideName_IsNotASeparator()
    {
        Assert.Equal("my-file.cs", TitleParser.Parse("my-file.cs - proj - App").File);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void EmptyTitle_YieldsEmptyParts(string? title)
    {
        var p = TitleParser.Parse(title);
        Assert.Equal("", p.File);
        Assert.Equal("", p.Workspace);
    }
}

public class TemplateEngineTests
{
    private static readonly TemplateContext Ctx = TemplateContext.Sample;

    [Fact]
    public void Tokens_AreExpanded() =>
        Assert.Equal("Editing Program.cs in my-project", TemplateEngine.Render("Editing {file} in {workspace}", Ctx));

    [Fact]
    public void OptionalSection_KeptWhenTokensHaveValues() =>
        Assert.Equal("Editing Program.cs", TemplateEngine.Render("[Editing {file}]", Ctx));

    [Fact]
    public void OptionalSection_DroppedWhenTokenEmpty()
    {
        var ctx = Ctx with { File = "" };
        Assert.Equal("Visual Studio Code", TemplateEngine.Render("Visual Studio Code[ - {file}]", ctx));
    }

    [Fact]
    public void UnknownToken_ExpandsToNothing() =>
        Assert.Equal("Hi there", TemplateEngine.Render("Hi {nope}there", Ctx));

    [Fact]
    public void TooShortResult_IsNull() =>
        Assert.Null(TemplateEngine.Render("[{file}]", Ctx with { File = "" }));

    [Fact]
    public void LongResult_IsTruncatedToDiscordLimit()
    {
        var result = TemplateEngine.Render(new string('x', 300), Ctx);
        Assert.Equal(TemplateEngine.MaxLength, result!.Length);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void EmptyTemplate_IsNull(string? t) => Assert.Null(TemplateEngine.Render(t, Ctx));
}

public class RuleMatcherTests
{
    private static DetectionRule Rule(string name, string procs, string regex = "", bool enabled = true) =>
        new() { Id = name, Name = name, ProcessNames = procs, TitleRegex = regex, Enabled = enabled };

    private static RunningProcess Proc(string name, int pid = 1, string title = "", bool fg = false, int ageMin = 10) =>
        new(name, pid, title, DateTime.UtcNow.AddMinutes(-ageMin), fg);

    [Fact]
    public void ParseProcessNames_NormalisesCaseAndExe() =>
        Assert.Equal(new[] { "code", "code - insiders" }, RuleMatcher.ParseProcessNames("Code.exe, CODE - Insiders ; code"));

    [Fact]
    public void Match_IgnoresDisabledRules()
    {
        var m = RuleMatcher.FindMatches(new[] { Rule("vs", "code", enabled: false) }, new[] { Proc("Code") });
        Assert.Empty(m);
    }

    [Fact]
    public void Match_RespectsTitleRegex()
    {
        var rules = new[] { Rule("web", "code", @"\.html?\b") };
        Assert.Empty(RuleMatcher.FindMatches(rules, new[] { Proc("Code", title: "main.cs - x") }));
        Assert.Single(RuleMatcher.FindMatches(rules, new[] { Proc("Code", title: "index.html - x") }));
    }

    [Fact]
    public void InvalidRegex_IsTreatedAsMatchAny()
    {
        Assert.Single(RuleMatcher.FindMatches(new[] { Rule("a", "code", "([") }, new[] { Proc("Code") }));
        Assert.False(RuleMatcher.IsValidTitleRegex("(["));
        Assert.True(RuleMatcher.IsValidTitleRegex(""));
    }

    [Fact]
    public void Winner_PrefersFocusedApp_OverRuleOrder()
    {
        var rules = new[] { Rule("vs", "devenv"), Rule("code", "code") };
        var running = new[] { Proc("devenv", 1), Proc("Code", 2, fg: true) };
        var matches = RuleMatcher.FindMatches(rules, running);

        Assert.Equal("code", RuleMatcher.PickWinner(matches, preferForeground: true)!.Rule.Name);
        Assert.Equal("vs", RuleMatcher.PickWinner(matches, preferForeground: false)!.Rule.Name);
    }

    [Fact]
    public void Winner_FallsBackToRuleOrder_WhenNothingFocused()
    {
        var rules = new[] { Rule("vs", "devenv"), Rule("code", "code") };
        var matches = RuleMatcher.FindMatches(rules, new[] { Proc("Code", 2), Proc("devenv", 1) });
        Assert.Equal("vs", RuleMatcher.PickWinner(matches, true)!.Rule.Name);
    }

    [Fact]
    public void SameRule_PrefersInstanceWithWindowTitle()
    {
        // Electron apps run many helper processes; only one has a window.
        var rules = new[] { Rule("code", "code") };
        var running = new[] { Proc("Code", 10, ageMin: 30), Proc("Code", 11, title: "a.cs - p - Visual Studio Code", ageMin: 5) };
        Assert.Equal(11, RuleMatcher.FindMatches(rules, running)[0].Process.Pid);
    }

    [Fact]
    public void NoMatches_NoWinner() =>
        Assert.Null(RuleMatcher.PickWinner(RuleMatcher.FindMatches(new[] { Rule("a", "code") }, new[] { Proc("notepad") }), true));
}
