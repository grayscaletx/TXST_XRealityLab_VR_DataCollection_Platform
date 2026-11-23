using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace XRealityLab.Security
{
    public class GoogleReCAPTCHAService
    {
        private ReCAPTCHASettings _settings;

        public GoogleReCAPTCHAService(IOptions<ReCAPTCHASettings> settings)
        {
            _settings = settings.Value;
        }

        /// <summary>reCAPTCHA is only used when both keys are configured.</summary>
        public bool IsEnabled =>
            !string.IsNullOrWhiteSpace(_settings.ReCAPTCHA_Site_Key) && !string.IsNullOrWhiteSpace(_settings.ReCAPTCHA_Secret_Key);

        /// <summary>True when the request may continue (reCAPTCHA disabled, or the token was accepted by Google).</summary>
        public async Task<bool> IsHumanAsync(string token)
        {
            if (!IsEnabled) return true;
            if (string.IsNullOrWhiteSpace(token)) return false;
            try
            {
                var result = await VerifyReCAPTCHA(token);
                return result != null && result.success && result.score >= 0.5;
            }
            catch (Exception ex)
            {
                // Google not reachable: do not lock everybody out of the platform.
                Console.WriteLine("[reCAPTCHA] Verification failed: " + ex.Message);
                return true;
            }
        }

        public virtual async Task<GoogleCAPTCHAResponse> VerifyReCAPTCHA(string _Token)
        {
            GoogleReCAPTCHAData _MyData = new GoogleReCAPTCHAData
            {
                response = _Token,
                secret = _settings.ReCAPTCHA_Secret_Key
            };

            HttpClient client = new HttpClient();

            var response = await client.GetStringAsync($"https://www.google.com/recaptcha/api/siteverify?secret={_MyData.secret}&response={_MyData.response}");

            var capresp = JsonConvert.DeserializeObject<GoogleCAPTCHAResponse>(response);

            return capresp;
        }
    }
    public class GoogleReCAPTCHAData
    {
        public string response { get; set; }//Token
        public string secret { get; set; }
    }

    public class GoogleCAPTCHAResponse
    {
        public bool success { get; set; }
        public double score { get; set; }
        public string action { get; set; }
        public DateTime challenge_ts { get; set; }
        public string hostname { get; set; }
    }
}
