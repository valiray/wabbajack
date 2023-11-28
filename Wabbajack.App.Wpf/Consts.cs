using System;
using Wabbajack.Paths;
using Wabbajack.Paths.IO;

namespace Wabbajack;

public static class Consts
{
    public static RelativePath MO2IniName = "ModOrganizer.ini".ToRelativePath();
    public static string AppName = "Wabbajack";
    public static Uri WabbajackBuildServerUri => new("https://build.wabbajack.org");
    public static Uri WabbajackModlistWizardUri => new("https://wizard.wabbajack.org");
    public static Uri WabbajackGithubUri => new("https://github.com/wabbajack-tools/wabbajack");
    public static Uri WabbajackDiscordUri => new("https://discord.gg/wabbajack");
    public static Uri WabbajackPatreonUri => new("https://www.patreon.com/user?u=11907933");
    public static Uri WabbajackWikiUri => new("https://wiki.wabbajack.org");
    public static Version CurrentMinimumWabbajackVersion { get; set; } = Version.Parse("2.3.0.0");
    public static bool UseNetworkWorkaroundMode { get; set; } = false;
    public static AbsolutePath CefCacheLocation { get; } = KnownFolders.WabbajackAppLocal.Combine("Cef");
    public static RelativePath ModListTxt { get; } = "modlist.txt".ToRelativePath();
    public static RelativePath CompilerSettings { get; } = "compiler_settings.json".ToRelativePath();

    public static byte SettingsVersion = 0;

    public static RelativePath NativeSettingsJson = "native_settings.json".ToRelativePath();
}