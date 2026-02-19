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

        public CarFeedbackService(IRepository<CarFeedback> feedbackRepository, IHttpContextAccessor httpContextAccessor)
        {
            _feedbackRepository = feedbackRepository;
            _httpContextAccessor = httpContextAccessor;
        }

        public CarFeedbackDto Add(CarFeedbackDto item)
        {
            var userIdStr = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            int userId = int.Parse(userIdStr ?? "0");

            if (userId == 0)
                throw new Exception("Unauthorized");

            CarFeedback newFeedback = new CarFeedback
            {
                Rating = item.Rating,
                UserComment = item.UserComment,
                DateCreated = DateTime.Now,
                CarId = item.CarId,
                UserId = userId,
                ReportedIssue = false
            };

            var saved = _feedbackRepository.Add(newFeedback);

            return new CarFeedbackDto
            {
                Id = saved.Id,
                Rating = saved.Rating,
                UserComment = saved.UserComment,
                DateCreated = saved.DateCreated,
                CarId = saved.CarId,
                UserName = item.UserName 
            };
        }

        public IEnumerable<CarFeedbackDto> GetAll()
        {
            return _feedbackRepository.GetAll()
                .OrderByDescending(f => f.DateCreated)
                .Select(f => new CarFeedbackDto
                {
                    Id = f.Id,
                    Rating = f.Rating,
                    UserComment = f.UserComment,
                    DateCreated = f.DateCreated,
                    CarId = f.CarId,
                    UserName = f.User != null ? f.User.FirstName : null
                }).ToList();
        }

        public CarFeedbackDto? GetById(int id)
        {
            var f = _feedbackRepository.GetById(id);
            if (f == null) return null;

            return new CarFeedbackDto
            {
                Id = f.Id,
                Rating = f.Rating,
                UserComment = f.UserComment,
                DateCreated = f.DateCreated,
                CarId = f.CarId,
                UserName = f.User != null ? f.User.FirstName : null
            };
        }

        public bool Update(int id, CarFeedbackDto item)
        {
            if (!Exists(id)) return false;

            var currentUserId = int.Parse(_httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var existing = _feedbackRepository.GetById(id);

            if (existing.UserId != currentUserId && _httpContextAccessor.HttpContext?.User.IsInRole("admin") == false)
                throw new Exception("Forbidden");

            existing.Rating = item.Rating;
            existing.UserComment = item.UserComment;
            // כאן נעדכן רק את השדות שמשתנים

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
                throw new Exception("זה לא המשוב שלך - אין לך רשות למחוק אותו");
            }
            return _feedbackRepository.Delete(id);
        }
        public bool Exists(int id)
        {
            return _feedbackRepository.Exists(id);
        }
    }
}
