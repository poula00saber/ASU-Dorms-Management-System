using ASUDorms.Domain.Entities;
using ASUDorms.Domain.Interfaces;
using ASUDorms.Infrastructure.Data;
using Microsoft.EntityFrameworkCore.Storage;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASUDorms.Infrastructure.Repositories
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly ApplicationDbContext _context;
        private IDbContextTransaction _transaction;

        public UnitOfWork(ApplicationDbContext context)
        {
            _context = context;

            DormLocations = new Repository<DormLocation>(_context);
            Users = new Repository<AppUser>(_context);
            Students = new Repository<Student>(_context);
            Holidays = new Repository<Holiday>(_context);
            MealTypes = new Repository<MealType>(_context);
            MealTransactions = new Repository<MealTransaction>(_context);
        }

        public IRepository<DormLocation> DormLocations { get; }
        public IRepository<AppUser> Users { get; }
        public IRepository<Student> Students { get; }
        public IRepository<Holiday> Holidays { get; }
        public IRepository<MealType> MealTypes { get; }
        public IRepository<MealTransaction> MealTransactions { get; }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task BeginTransactionAsync()
        {
            _transaction = await _context.Database.BeginTransactionAsync();
        }

        public async Task CommitTransactionAsync()
        {
            try
            {
                await _context.SaveChangesAsync();
                if (_transaction != null)
                {
                    await _transaction.CommitAsync();
                }
            }
            catch
            {
                await RollbackTransactionAsync();
                throw;
            }
            finally
            {
                if (_transaction != null)
                {
                    await _transaction.DisposeAsync();
                    _transaction = null;
                }
            }
        }

        public async Task RollbackTransactionAsync()
        {
            if (_transaction != null)
            {
                await _transaction.RollbackAsync();
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }

        public void Dispose()
        {
            _transaction?.Dispose();
            _context?.Dispose();
        }
    }
}
