using GrpcSample.Domain.Exceptions;
using GrpcSample.Domain.Models;
using GrpcSample.Domain.Models.Customers;
using GrpcSample.Domain.Repositories.Abstractions;

namespace GrpcSample.Domain.Repositories.InMemory;

public sealed class InMemoryCustomerRepository(InMemoryCommerceStore store) : ICustomerRepository
{
    public Customer GetById(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new DomainValidationException("Customer ID is required.");

        if (store.Customers.TryGetValue(id.Trim(), out var customer))
            return customer;

        throw new DomainNotFoundException($"Customer '{id}' was not found.");
    }

    public int Count() => store.Customers.Count;

    public void RecordSpending(string customerId, decimal amount)
    {
        lock (store.CustomerLock)
        {
            var customer = GetById(customerId);
            customer.TotalSpent += amount;
        }
    }
}
