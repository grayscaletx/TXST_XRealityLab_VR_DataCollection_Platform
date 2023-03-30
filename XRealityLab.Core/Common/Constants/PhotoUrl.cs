using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace Common.Constants
{
    public class PhotoUrl
    {
        public static string ADMINISTRATOR_ICON_SVG = "/Images/icons/profile_icon_admin.svg";
        public static string USER_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_USER_ICONS, "user-stock-icon.png");
        public static string ADMINISTRATOR_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_USER_ICONS, "administrator-stock-icon.png");
        public static string DIRECTOR_OF_IT_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_USER_ICONS, "director-it-stock-icon.png");
        public static string LAB_DIRECTOR_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_USER_ICONS, "lab-director-stock-icon.png");
        public static string PRINCIPLE_INVESTIGATOR_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_USER_ICONS, "principle-investigator-stock-icon.png");
        public static string RESEARCH_ASSISTANT_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_USER_ICONS, "research-assistant-stock-icon.png");
        public static string ORGANIZATION_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_ORGANIZATION_ICONS, "organization-stock-icon.png");
        public static string LAB_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_LAB_ICONS, "lab-stock-icon.png");
        public static string PROJECT_ICON_PNG = Path.Combine(BaseLink.AZURE_BLOB_URL, AzureGPV2Credentials.CONTAINER_PROJECT_ICONS, "project-stock-icon.png");
        public static string DEFAULT_ICON = "https://atshero.com/Images/icons/profile_icon_trainee.svg";
    }

    public static class BaseLink
    {
        public static string AZURE_BLOB_URL = "https://xrealitylabstorage.blob.core.windows.net/";
    }
}
