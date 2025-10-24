namespace SignallingPowerApp.Core
{
    /// <summary>
    ///     Contains application version information.
    /// </summary>
    public static class AppVersion
    {
        private static readonly VersionInfo[] versions =
            [new VersionInfo(0, 1, new DateTime(2025, 10, 17), "Liam McGuire", "Initial version with basic functionality.")];
      
        public static VersionInfo CurrentVersion => versions[^1];
        public static VersionInfo[] VersionHistory => versions;
    }

    public record VersionInfo(int Major, int Minor, DateTime BuildDate, string Author, string Description);
}