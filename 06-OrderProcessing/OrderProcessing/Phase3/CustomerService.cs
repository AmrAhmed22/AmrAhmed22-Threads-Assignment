using OrderProcessing.Phase1;
using OrderProcessing.Shared.Exceptions;
using OrderProcessing.Shared.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderProcessing.Phase3
{

    public sealed class CustomerService
    {
        readonly CustomerRepository _customers;

        public CustomerService(CustomerRepository customers) => _customers = customers;

        public async Task<Customer> GetCustomerAsync(int id, CancellationToken ct)
        {
            Customer? customer = await _customers.GetByIdAsync(id, ct);
            return customer ?? throw new CustomerNotFoundException($"Customer #{id} not found");
        }
    }
}
