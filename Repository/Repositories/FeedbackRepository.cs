using Repository.Entities;
using Repository.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Repository.Repositories
{
    public class FeedbackRepository : IRepository<CarFeedback>
    {
        private readonly IContext context;

        public FeedbackRepository(IContext context)
        {
            this.context = context;
        }
        public CarFeedback Add(CarFeedback item)
        {
           context.Feedbacks.Add(item);
           context.Save();
           return item;
        }

        public bool Delete(int id)
        {
            var feedback = context.Feedbacks.Find(id);
            if (feedback != null)
            {
                context.Feedbacks.Remove(feedback);
                context.Save();
                return true;
            }
            return false;
        }

        public bool Exists(int id)
        {
            return context.Feedbacks.Any(x => x.Id == id);
        }

        public IEnumerable<CarFeedback> GetAll()
        {
            return context.Feedbacks.AsQueryable();
        }

        public CarFeedback? GetById(int id)
        {
            return context.Feedbacks.Find(id);
        }

        public bool Update(int id,CarFeedback item)
        {
            var existingFeedback = context.Feedbacks.Find(id);
            if (existingFeedback == null) return false;
            existingFeedback.Rating = item.Rating;
            existingFeedback.UserComment = item.UserComment;
            existingFeedback.ReportedIssue = item.ReportedIssue;
            context.Save();
            return true;
        }
    }
}
