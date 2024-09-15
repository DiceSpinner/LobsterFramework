namespace LobsterFramework
{
    internal static class Constants
    {
        public const string Framework = nameof(LobsterFramework);
        public const string MenuRootName = "Root";

        // Order for running initialization scripts, lower index value means higher priority (earlier initialization)
        public const int AttributeInitOrder = 1;
        public const int PlayerLoopInjectionOrder = 2;
    }
}
