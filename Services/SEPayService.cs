using Microsoft.Extensions.Options;
using NearGo.Configurations;

namespace NearGo.Services
{
    public class SEPayService
    {
        private readonly SEPaySettings _settings;

        public SEPayService(IOptions<SEPaySettings> settings)
        {
            _settings = settings.Value;
        }

        public string GetBankAccount() => _settings.BankAccount;
        public string GetBankName() => _settings.BankName;
        public string GetAccountHolder() => _settings.AccountHolder;

        public string GenerateTransferContent(string orderCode)
        {
            return $"SEVQR TKP{orderCode}";
        }

        public string GenerateQRUrl(string orderCode, decimal amount = 0)
        {
            var content = GenerateTransferContent(orderCode);
            var amountParam = amount > 0 ? $"&amount={amount:F0}" : "";
            return $"{_settings.QRBaseUrl}?acc={_settings.BankAccount}&bank={_settings.BankName}&des={Uri.EscapeDataString(content)}{amountParam}";
        }

        public string GenerateReturnUrl(string orderCode)
        {
            return $"{_settings.ReturnUrl}?orderCode={orderCode}";
        }

        public string GetWebhookToken() => _settings.WebhookToken;
    }
}
