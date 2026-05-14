using Common.Dto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Service.Interfaces
{
    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string subject, string htmlMessage);
        Task SendOrderConfirmationAsync(string userEmail, int orderId, string carModel);  // שליחת אישור הזמנה עם קישור לשאלון מצב הרכב
        Task SendFinalReceiptAsync(string userEmail, OrderDto orderDetails);        // שליחת קבלה מפורטת בסיום הנסיעה
        Task SendPasswordResetAsync(string userEmail, string resetCode);// שליחת קוד לאיפוס סיסמה
        Task SendWelcomeEmailAsync(string userEmail, string userName);
        Task SendFineNotificationAsync(string email, decimal fine, string fineReason);
        Task SendRegistrationVerificationAsync(string userEmail, string verificationCode);// שליחת קוד אימות בעת הרשמה
    }
}
