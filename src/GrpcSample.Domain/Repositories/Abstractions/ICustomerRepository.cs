using GrpcSample.Domain.Models.Customers;

namespace GrpcSample.Domain.Repositories.Abstractions;

public interface ICustomerRepository
{
    Customer GetById(string id);
    int Count();
    void RecordSpending(string customerId, decimal amount);
}
