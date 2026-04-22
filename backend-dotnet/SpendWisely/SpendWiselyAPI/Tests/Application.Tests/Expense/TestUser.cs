using SpendWiselyAPI.Domain.Entities;

namespace SpendWiselyAPI.Tests.Application.Tests.Expense
{
    public static class UserTestFactory
    {
        public static User Create(Guid id)
        {
            return new User("Test User", "email@test.com", "password@123");
        }
    }
}
