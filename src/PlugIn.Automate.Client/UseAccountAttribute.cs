namespace PlugIn.Automate.Client
{
    /// <summary>
    /// Decorates a test method or class with the account that should be automatically
    /// logged in before the test runs. Method-level takes precedence over class-level.
    /// Omit for tests that drive the login/register UI themselves.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public class UseAccountAttribute : Attribute
    {
        public AutomationAccount Account { get; }

        public UseAccountAttribute(AutomationAccount account)
        {
            Account = account;
        }
    }
}
