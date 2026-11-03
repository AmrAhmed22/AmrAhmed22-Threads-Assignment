using OrderProcessing.Shared.Logging;
using OrderProcessing.Shared.Models;
using OrderProcessing.Shared.Storage;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{
    public sealed class SyncEmailService
    {
        readonly JsonFileStore _store;

        public SyncEmailService(JsonFileStore store) => _store = store;

        public void SendConfirmation(Order order, Customer customer)
        {
            Log.Info($"SyncEmailService -> Order #{order.Id}: rendering template...");
            Thread.Sleep(200);

            Log.Info($"SyncEmailService -> Order #{order.Id}: connecting to SMTP...");
            Thread.Sleep(300);

            Log.Info($"SyncEmailService -> Order #{order.Id}: sending...");
            Thread.Sleep(500);

            _store.Modify<EmailMessage>(DataFiles.Emails, list => list.Add(new EmailMessage
            {
                Id = Guid.NewGuid(),
                OrderId = order.Id,
                To = customer.Email,
                Subject = $"Order #{order.Id} confirmed",
                Body = $"Hi {customer.Name}, your order #{order.Id} (total {order.TotalAmount}) is confirmed.",
                SentAt = DateTime.Now,
                Attempts = 1
            }));
        }
    }
}
