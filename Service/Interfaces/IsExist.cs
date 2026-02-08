using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Dto;

namespace Service.Interfaces
{
    public interface IsExist<T>
    {
        public T Exist(LoginDto l);

    }
}
