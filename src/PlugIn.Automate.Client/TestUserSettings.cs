namespace PlugIn.Automate.Client
{
    /// <summary>
    /// Credentials and display info for a single test account.
    /// Populated from the Pools section of testsettings.json / apitestsettings.json.
    /// </summary>
    public class TestUserSettings
    {
        public string Email       { get; set; } = string.Empty;
        public string Password    { get; set; } = string.Empty;
        public string Username    { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
    }
}
