using Common.Constants;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Mail
{
    public class MailService
    {
        SendGridClient client;
        SendGridMessage msg;

        public MailService()
        {
            client = new SendGridClient(SendGridCredentials.API_KEY);
            msg = new SendGridMessage();
            msg.SetFrom(new EmailAddress(SendGridCredentials.SET_FROM_EMAIL, SendGridCredentials.SET_FROM_NAME));
        }

        public async Task<bool> SendResetPasswordAsync(string resetLink, string email)
        {
            msg.AddTo(new EmailAddress(email, SendGridCredentials.SET_FROM_NAME));
            msg.AddSubstitution("-resetLink-", resetLink);
            msg.SetTemplateId(SendGridCredentials.TEMPLATE_FORGOT_PASSWORD_ID);
            msg.Subject = "Reset your XRealityLab password";

            var response = await client.SendEmailAsync(msg);
            Console.WriteLine(response.Headers.ToString());

            return (response.StatusCode == System.Net.HttpStatusCode.Accepted) ? true : false;
        }

        public async Task<bool> SendAccountCreationAsync(string firstname, string role, string password, string organizationName, string type, string email)
        {
            msg.AddTo(new EmailAddress(email, SendGridCredentials.SET_FROM_NAME));
            msg.AddSubstitution("-name-", firstname);
            msg.AddSubstitution("-role-", role);
            msg.AddSubstitution("-password-", password);
            msg.AddSubstitution("-organizationName-", organizationName);
            msg.AddSubstitution("-type-", type);
            msg.SetTemplateId(SendGridCredentials.TEMPLATE_PLATFORM_INVITATION_ID);
            msg.Subject = "Log In To your XRealityLab Platform Account";

            var response = await client.SendEmailAsync(msg);
            Console.WriteLine(response.Headers.ToString());

            return (response.StatusCode == System.Net.HttpStatusCode.Accepted) ? true : false;
        }
    }
}
