using Common.Dto;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class OrderService : IOrderService
    {
        public OrderDto Add(OrderDto item)
        {
            throw new NotImplementedException();
        }

        public bool Delete(int id)
        {
            throw new NotImplementedException();
        }

        public bool Exists(int id)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<OrderDto> GetAll()
        {
            throw new NotImplementedException();
        }

        public OrderDto? GetById(int id)
        {
            throw new NotImplementedException();
        }

        public bool Update(int id, OrderDto item)
        {
            throw new NotImplementedException();
        }
    }
}
