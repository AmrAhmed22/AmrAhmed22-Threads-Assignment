using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase4
{
    public interface IShippingCarrier
    {
        string Name { get; }
        Task<decimal> GetQuoteAsync(Order order, CancellationToken ct);
    }
}
