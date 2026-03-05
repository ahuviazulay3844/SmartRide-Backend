using AutoMapper;
using Common.Dto;
using Microsoft.AspNetCore.Http;
using Repository.Entities;
using Repository.Interfaces;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class CarFeedbackService : ICarFeedbackService
    {
        private readonly IRepository<CarFeedback> _feedbackRepository;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IMapper _mapper;

        public CarFeedbackService(IRepository<CarFeedback> feedbackRepository, IHttpContextAccessor httpContextAccessor, IMapper mapper)
        {
            _feedbackRepository = feedbackRepository;
            _httpContextAccessor = httpContextAccessor;
            _mapper = mapper;
        }

        public async Task<CarFeedbackDto> Add(CarFeedbackDto item)
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = int.Parse(userIdStr ?? "0");

            if (userId == 0)
               return null;
            CarFeedback newFeedback = _mapper.Map<CarFeedback>(item);
            newFeedback.DateCreated = DateTime.Now;
            newFeedback.UserId = userId;
            var saved = _feedbackRepository.Add(newFeedback);
            return _mapper.Map<CarFeedbackDto>(saved);
        }

        public IEnumerable<CarFeedbackDto> GetAll()
        {
            var feedbacks = _feedbackRepository.GetAll().OrderByDescending(f => f.DateCreated);
    
            return _mapper.Map<IEnumerable<CarFeedbackDto>>(feedbacks);
        }

        public CarFeedbackDto? GetById(int id)
        {
            var f = _feedbackRepository.GetById(id);
            if (f == null) return null;
            return _mapper.Map<CarFeedbackDto>(f);
        }

        public bool Update(int id, CarFeedbackDto item)
        {
            if (!Exists(id)) return false;

            var currentUserId = int.Parse(_httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var existing = _feedbackRepository.GetById(id);

            if (existing.UserId != currentUserId && _httpContextAccessor.HttpContext?.User.IsInRole("admin") == false)
                return false;

            _mapper.Map(item, existing);
            _feedbackRepository.Update(id, existing);
            return true;
        }
        public bool Delete(int id)
        {
            var feedback = _feedbackRepository.GetById(id);
            if (feedback == null) return false;
            var currentUserId = int.Parse(_httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            if (feedback.UserId != currentUserId)
            {
                return false;
            }
            return _feedbackRepository.Delete(id);
        }
        public bool Exists(int id)
        {
            return _feedbackRepository.Exists(id);
        }

        public IEnumerable<CarFeedbackDto> GetByIdOfCar(int carId)
        {
            var feedbacks=_feedbackRepository.GetAll().Where(x=>x.CarId==carId).OrderByDescending(f => f.DateCreated);
            return _mapper.Map<IEnumerable<CarFeedbackDto>>(feedbacks);
        }

        public IEnumerable<CarFeedbackDto> GetByIdOfUser(int userId)
        {
            var feedbacks = _feedbackRepository.GetAll().Where(x => x.UserId == userId).OrderByDescending(f => f.DateCreated);
            return _mapper.Map<IEnumerable<CarFeedbackDto>>(feedbacks);
        }
    }
}
