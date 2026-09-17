using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace MVCSampleApp.Services
{
    public interface ISmsSender
    {
        Task SendSmsAsync(string phoneNumber, string message);
    }

    // Sends the 2FA code over SMS using Twilio.
    // Sign up for a free Twilio trial account to get these three values.
    public class SmsSender : ISmsSender
    {
        private readonly IConfiguration _configuration;

        public SmsSender(IConfiguration configuration)
        {
            _configuration = configuration;
            TwilioClient.Init(_configuration["Twilio:AccountSid"], _configuration["Twilio:AuthToken"]);
        }

        public async Task SendSmsAsync(string phoneNumber, string message)
        {
            await MessageResource.CreateAsync(
                body: message,
                from: new PhoneNumber(_configuration["Twilio:FromNumber"]),
                to: new PhoneNumber(phoneNumber)
            );
        }
    }
}
