using Common.Constants;
using MimeKit;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Mail
{
    /// <summary>
    /// Sends the platform e-mails (invitation with temporary password, password reset link).
    /// The provider is chosen in configuration (Mail:Provider = Webhook | SendGrid | Smtp | None).
    /// With "None" the e-mail is written to the application log, so a fresh installation
    /// works without any mail account.
    /// E-mail bodies are built here, so no SendGrid templates are required.
    /// </summary>
    public class MailService
    {
        private static readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

        public async Task<bool> SendResetPasswordAsync(string resetLink, string email)
        {
            string html =
                "<p>Hello,</p>" +
                "<p>We received a request to reset the password of your XRealityLab account.</p>" +
                "<p><a href=\"" + WebUtility.HtmlEncode(resetLink) + "\">Click here to choose a new password</a></p>" +
                "<p>If the link does not open, copy this address into your browser:<br/>" + WebUtility.HtmlEncode(resetLink) + "</p>" +
                "<p>If you did not ask for this, you can ignore this e-mail.</p>" +
                "<p>" + WebUtility.HtmlEncode(SendGridCredentials.SET_FROM_NAME) + "</p>";

            return await SendAsync(email, "Reset your XRealityLab password", html);
        }

        public async Task<bool> SendAccountCreationAsync(string firstname, string role, string password, string organizationName, string type, string email)
        {
            string platform = string.IsNullOrWhiteSpace(SendGridCredentials.PLATFORM_URL)
                ? string.Empty
                : "<p>Platform address: <a href=\"" + WebUtility.HtmlEncode(SendGridCredentials.PLATFORM_URL) + "\">" + WebUtility.HtmlEncode(SendGridCredentials.PLATFORM_URL) + "</a></p>";

            string html =
                "<p>Hello " + WebUtility.HtmlEncode(firstname) + ",</p>" +
                "<p>An XRealityLab account was created for you as <b>" + WebUtility.HtmlEncode(role) + "</b> of the " +
                WebUtility.HtmlEncode(type) + " <b>" + WebUtility.HtmlEncode(organizationName) + "</b>.</p>" +
                "<p>E-mail: " + WebUtility.HtmlEncode(email) + "<br/>Temporary password: <b>" + WebUtility.HtmlEncode(password) + "</b></p>" +
                platform +
                "<p>Please log in and change your password (profile icon &gt; Change Password) within 10 days. " +
                "After 10 days the temporary password expires and you will need the \"Forgot your password?\" link.</p>" +
                "<p>" + WebUtility.HtmlEncode(SendGridCredentials.SET_FROM_NAME) + "</p>";

            return await SendAsync(email, "Log in to your XRealityLab platform account", html);
        }

        /// <summary>Encodes a value as a JSON string literal (quotes, backslashes and control characters escaped).</summary>
        private static string JsonString(string value)
        {
            var sb = new StringBuilder("\"");
            foreach (char c in value ?? string.Empty)
            {
                if (c == '"') sb.Append("\\\"");
                else if (c == '\\') sb.Append("\\\\");
                else if (c < ' ') sb.Append("\\u").Append(((int)c).ToString("x4"));
                else sb.Append(c);
            }
            return sb.Append('"').ToString();
        }

        private static async Task<bool> SendAsync(string to, string subject, string html)
        {
            try
            {
                string provider = (SendGridCredentials.PROVIDER ?? "None").Trim().ToLowerInvariant();

                if (provider == "webhook" && !string.IsNullOrWhiteSpace(SendGridCredentials.WEBHOOK_URL))
                {
                    string json = "{\"to\":" + JsonString(to) + ",\"subject\":" + JsonString(subject) + ",\"html\":" + JsonString(html) + "}";
                    using (var request = new HttpRequestMessage(HttpMethod.Post, SendGridCredentials.WEBHOOK_URL))
                    {
                        request.Content = new StringContent(json, Encoding.UTF8, "application/json");
                        request.Headers.Add("X-XRealityLab-Key", SendGridCredentials.WEBHOOK_KEY ?? string.Empty);
                        using (var response = await http.SendAsync(request))
                        {
                            if (!response.IsSuccessStatusCode)
                                Console.WriteLine("[Mail] Webhook refused the e-mail to " + to + ": " + (int)response.StatusCode);
                            return response.IsSuccessStatusCode;
                        }
                    }
                }

                if (provider == "sendgrid" && !string.IsNullOrWhiteSpace(SendGridCredentials.API_KEY))
                {
                    var client = new SendGridClient(SendGridCredentials.API_KEY);
                    var msg = MailHelper.CreateSingleEmail(
                        new EmailAddress(SendGridCredentials.SET_FROM_EMAIL, SendGridCredentials.SET_FROM_NAME),
                        new EmailAddress(to), subject, null, html);
                    var response = await client.SendEmailAsync(msg);
                    bool ok = response.StatusCode == HttpStatusCode.Accepted;
                    if (!ok) Console.WriteLine("[Mail] SendGrid refused the e-mail to " + to + ": " + response.StatusCode);
                    return ok;
                }

                if (provider == "smtp" && !string.IsNullOrWhiteSpace(SendGridCredentials.SMTP_HOST))
                {
                    var message = new MimeMessage();
                    message.From.Add(new MailboxAddress(SendGridCredentials.SET_FROM_NAME, SendGridCredentials.SET_FROM_EMAIL));
                    message.To.Add(new MailboxAddress(to, to));
                    message.Subject = subject;
                    message.Body = new BodyBuilder { HtmlBody = html }.ToMessageBody();

                    using (var smtp = new MailKit.Net.Smtp.SmtpClient())
                    {
                        var security = SendGridCredentials.SMTP_USE_SSL
                            ? MailKit.Security.SecureSocketOptions.Auto
                            : MailKit.Security.SecureSocketOptions.None;
                        await smtp.ConnectAsync(SendGridCredentials.SMTP_HOST, SendGridCredentials.SMTP_PORT, security);
                        if (!string.IsNullOrWhiteSpace(SendGridCredentials.SMTP_USER))
                            await smtp.AuthenticateAsync(SendGridCredentials.SMTP_USER, SendGridCredentials.SMTP_PASSWORD);
                        await smtp.SendAsync(message);
                        await smtp.DisconnectAsync(true);
                    }
                    return true;
                }

                // No mail provider configured: keep the content available to the installer in the log.
                Console.WriteLine("[Mail] (no provider configured) To: " + to + " | Subject: " + subject);
                Console.WriteLine("[Mail] " + html);
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("[Mail] Sending to " + to + " failed: " + ex.Message);
                return false;
            }
        }
    }
}
