using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Dto; 
namespace Service.Interfaces
{
    public interface IService<T>
    {
        IEnumerable<T> GetAll();
        T? GetById(int id);
        T Add(T item);
        bool Update(int id, T item);
        bool Delete(int id);
        bool Exists(int id);
    }
}
