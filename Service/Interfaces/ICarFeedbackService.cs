using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common.Dto; 
namespace Service.Interfaces
{
    public interface ICarFeedbackService: IService<CarFeedbackDto>
    {
        IEnumerable<CarFeedbackDto> GetByIdOfCar(int carId);
        IEnumerable<CarFeedbackDto> GetByIdOfUser(int userId);
    }
}
