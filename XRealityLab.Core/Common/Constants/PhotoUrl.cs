namespace Common.Constants
{
    public class PhotoUrl
    {
        public static string USER_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_USER_ICONS, "user-stock-icon.png");
        public static string ADMINISTRATOR_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_USER_ICONS, "administrator-stock-icon.png");
        public static string DIRECTOR_OF_IT_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_USER_ICONS, "director-it-stock-icon.png");
        public static string LAB_DIRECTOR_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_USER_ICONS, "lab-director-stock-icon.png");
        public static string PRINCIPLE_INVESTIGATOR_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_USER_ICONS, "principle-investigator-stock-icon.png");
        public static string RESEARCH_ASSISTANT_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_USER_ICONS, "research-assistant-stock-icon.png");
        public static string ORGANIZATION_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_ORGANIZATION_ICONS, "organization-stock-icon.png");
        public static string LAB_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_LAB_ICONS, "lab-stock-icon.png");
        public static string PROJECT_ICON_PNG => BaseLink.Combine(AzureGPV2Credentials.CONTAINER_PROJECT_ICONS, "project-stock-icon.png");
    }

    public static class BaseLink
    {
        /// <summary>Public base URL of stored files, e.g. "https://account.blob.core.windows.net/" or "/files/".</summary>
        public static string AZURE_BLOB_URL { get; set; } = "/files/";

        /// <summary>Builds a file URL with forward slashes on every OS (Path.Combine must not be used for URLs).</summary>
        public static string Combine(string container, string fileName)
        {
            return AZURE_BLOB_URL.TrimEnd('/') + "/" + container.Trim('/') + "/" + fileName;
        }
    }
}
