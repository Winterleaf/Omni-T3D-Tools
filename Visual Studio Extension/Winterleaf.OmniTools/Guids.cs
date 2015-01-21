using System;

namespace Winterleaf.OmniTools
{
    internal static class GuidList
    {
        public const string guidOmniToolsPkgString = "5dd9aab8-1767-4054-9fcb-02c50c4f5051";
        public const string guidOmniToolsCmdSetString = "8D7B9CB3-3591-47f9-B104-B7EB173E0F03";
        public const string guidStaticCodeGenerationCmdSetString = "B0BDE464-54B1-47E5-B8F1-F29217456B2B";

        public const string guidAutoGenConverterCmdSetString = "2FA21354-3080-46DE-9274-DBBAC52A1427";

        public const string guidOmniToolsAboutCmdSetString = "9AD293BA-AC31-4E46-A206-ABEE5C465726";

        public static readonly Guid guidOmniToolsCmdSet = new Guid(guidOmniToolsCmdSetString);
        public static readonly Guid guidStaticCodeGenerationCmdSet = new Guid(guidStaticCodeGenerationCmdSetString);
        public static readonly Guid guidAutoGenConverterCmdSet = new Guid(guidAutoGenConverterCmdSetString);
        public static readonly Guid guidOmniToolsAboutCmdSet = new Guid(guidOmniToolsAboutCmdSetString);
    };
}