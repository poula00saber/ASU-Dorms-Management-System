using ASUDorms.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Domain.Interfaces
{
    public interface IUnitOfWork : IDisposable
    {
        IRepository<DormLocation> DormLocations { get; }
        IRepository<AppUser> Users { get; }
        IRepository<Student> Students { get; }
        IRepository<Holiday> Holidays { get; }
        IRepository<MealType> MealTypes { get; }
        IRepository<MealTransaction> MealTransactions { get; }
         IRepository<PaymentExemption> PaymentExemptions { get; }
        IRepository<PaymentTransaction> PaymentTransactions { get; }
        Task<int> SaveChangesAsync();
        Task BeginTransactionAsync();
        Task CommitTransactionAsync();
        Task RollbackTransactionAsync();
    }
}
