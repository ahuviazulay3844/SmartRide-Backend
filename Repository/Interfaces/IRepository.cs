using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Interfaces
{
    public interface IRepository<T>
    {
        IEnumerable<T> GetAll();
        T? GetById(int id);
        T Add(T item);
        bool Update(int id, T item);
        bool Delete(int id);
        bool Exists(int id);
    }
}
