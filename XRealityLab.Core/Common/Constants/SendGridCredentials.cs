namespace Common.Constants
{
    /// <summary>
    /// E-mail settings, loaded from configuration at startup (section "Mail").
    /// Provider: "Webhook", "SendGrid", "Smtp" or "None" (None = e-mails are written to the application log).
    /// </summary>
    public static class SendGridCredentials
    {
        public static string PROVIDER { get; set; } = "None";
        public static string API_KEY { get; set; } = string.Empty;
        public static string SET_FROM_EMAIL { get; set; } = "no-reply@localhost";
        public static string SET_FROM_NAME { get; set; } = "XRealityLab team";

        public static string SMTP_HOST { get; set; } = string.Empty;
        public static int SMTP_PORT { get; set; } = 587;
        public static string SMTP_USER { get; set; } = string.Empty;
        public static string SMTP_PASSWORD { get; set; } = string.Empty;
        public static bool SMTP_USE_SSL { get; set; } = true;

        /// <summary>Webhook provider: the platform POSTs {to, subject, html} as JSON to this URL (for example an n8n workflow).</summary>
        public static string WEBHOOK_URL { get; set; } = string.Empty;
        /// <summary>Sent in the X-XRealityLab-Key header so the receiver can reject other callers.</summary>
        public static string WEBHOOK_KEY { get; set; } = string.Empty;

        /// <summary>Public address of the web platform, used in invitation e-mails.</summary>
        public static string PLATFORM_URL { get; set; } = string.Empty;
    }
}
