namespace Common.Constants
{
    /// <summary>
    /// File storage settings. Values are loaded from configuration at startup
    /// (section "Storage"); nothing secret lives in source code any more.
    /// If no Azure connection string is configured, files are stored on local disk
    /// under LOCAL_STORAGE_ROOT and served by the web platform itself.
    /// </summary>
    public class AzureGPV2Credentials
    {
        public const string CONTAINER_USER_ICONS = "user-icons";
        public const string CONTAINER_ORGANIZATION_ICONS = "organization-icons";
        public const string CONTAINER_LAB_ICONS = "lab-icons";
        public const string CONTAINER_PROJECT_ICONS = "project-icons";
        public const string CONTAINER_RESEARCH_FILES = "research-files";
        public const string CONTAINER_PDF_FILES = "pdf-files";

        public static string STORAGE_ACCOUNT_CONNECTION_STRING { get; set; } = string.Empty;
        public static string LOCAL_STORAGE_ROOT { get; set; } = string.Empty;

        public static bool UseAzure => !string.IsNullOrWhiteSpace(STORAGE_ACCOUNT_CONNECTION_STRING);
    }
}
