using OrderProcessing.Phase1;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{

    public sealed class FireAndForgetDemo
    {
        readonly OrderRepository _orderRepository;
        readonly IEmailService _emailService;

        public FireAndForgetDemo(OrderRepository orderRepository, IEmailService emailService)
        {
            _orderRepository = orderRepository;
            _emailService = emailService;
        }

        // the broken method, exactly as given
#pragma warning disable CS4014
        public async Task ProcessOrderAsync(Order order, Customer customer)
        {
            await _orderRepository.SaveAsync(order);
            _emailService.SendConfirmationAsync(order, customer); // not awaited
            Console.WriteLine("Order processing finished");
        }
#pragma warning restore CS4014

        // the fix: await the email
        public async Task ProcessOrderFixedAsync(Order order, Customer customer, CancellationToken ct)
        {
            await _orderRepository.SaveAsync(order, ct);
            await _emailService.SendConfirmationAsync(order, customer, ct);
            Console.WriteLine("Order processing finished");
        }
    }
}
