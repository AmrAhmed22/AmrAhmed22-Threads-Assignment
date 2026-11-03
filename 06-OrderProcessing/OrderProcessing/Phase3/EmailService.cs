using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class EmailService : IEmailService
    {
        readonly JsonFileStore _store;
        readonly Dictionary<int, int> _failAttempts;                   
        readonly ConcurrentDictionary<int, int> _attempts = new();      

        public EmailService(JsonFileStore store, Dictionary<int, int>? failAttempts = null)
        {
            _store = store;
            _failAttempts = failAttempts ?? new Dictionary<int, int>();
        }

        public async Task SendConfirmationAsync(Order order, Customer customer, CancellationToken ct = default)
        {
            int attempt = _attempts.AddOrUpdate(order.Id, 1, (_, old) => old + 1);

            Log.Info($"EmailService -> Order #{order.Id}: rendering template (attempt {attempt})...");
            await Task.Delay(200, ct);

            Log.Info($"EmailService -> Order #{order.Id}: connecting to SMTP...");
            await Task.Delay(300, ct);

            if (_failAttempts.TryGetValue(order.Id, out int failCount) && attempt <= failCount)
                throw new SmtpUnavailableException($"SMTP server unavailable (attempt {attempt})");

            Log.Info($"EmailService -> Order #{order.Id}: sending...");
            await Task.Delay(500, ct);

            var email = new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} confirmed",
                Body = $"Hi {customer.Name}, your order #{order.Id} (total {order.TotalAmount}) is confirmed.",
                SentAt = DateTime.Now,
                Attempts = attempt
            };

            await _store.ModifyAsync<EmailMessage>(DataFiles.Emails, list => list.Add(email), ct);
        }
    }
}
