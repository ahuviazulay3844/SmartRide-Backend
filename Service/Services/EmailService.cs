using Common.Dto;
using Microsoft.Extensions.Options;
using Repository.Entities;
using Service.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace Service.Services
{
    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private const string PrimaryColor = "#6366f1"; // Indigo
        private const string SecondaryColor = "#1e293b"; // Slate
        private const string SuccessColor = "#10b981"; // Emerald
        private const string DangerColor = "#ef4444"; // Rose

        public EmailService(IOptions<EmailSettings> options)
        {
            _settings = options.Value;
        }

        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var client = new SmtpClient(_settings.SmtpServer, _settings.Port)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(_settings.SenderEmail, _settings.SenderPassword)
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.SenderEmail, "CityCar Premium"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true
            };

            mailMessage.To.Add(toEmail);
            await client.SendMailAsync(mailMessage);
        }

        public async Task SendOrderConfirmationAsync(string userEmail, int orderId, string carModel)
        {
            string message = $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, sans-serif; max-width: 600px; margin: auto; border-radius: 16px; overflow: hidden; box-shadow: 0 4px 15px rgba(0,0,0,0.1); border: 1px solid #e2e8f0;'>
                <div style='background-color: {PrimaryColor}; padding: 30px; text-align: center;'>
                    <h1 style='color: white; margin: 0; font-size: 24px;'>הזמנתך אושרה!</h1>
                </div>
                <div style='padding: 30px; background-color: white;'>
                    <p style='font-size: 18px; color: {SecondaryColor};'>שלום רב,</p>
                    <p>אנו שמחים לאשר את הזמנתך מספר <b>#{orderId}</b>.</p>
                    <div style='background-color: #f8fafc; padding: 20px; border-radius: 12px; margin: 20px 0; border-right: 4px solid {PrimaryColor};'>
                        <p style='margin: 0;'><b>רכב מוזמן:</b> {carModel}</p>
                    </div>
                    <p>לפני הכניסה לרכב, חובה למלא את שאלון מצב הרכב כדי לשמור על הכיסוי הביטוחי שלך:</p>
                    <div style='text-align: center; margin-top: 30px;'>
                        <a href='https://citycar.co.il/survey?orderId={orderId}' style='background-color: {PrimaryColor}; color: white; padding: 14px 35px; text-decoration: none; border-radius: 50px; font-weight: bold; display: inline-block;'>למילוי דיווח מצב רכב</a>
                    </div>
                </div>
            </div>";

            await SendEmailAsync(userEmail, $"אישור הזמנה #{orderId} - CityCar", message);
        }

        public async Task SendFinalReceiptAsync(string userEmail, OrderDto orderDetails)
        {
            string message = $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, sans-serif; max-width: 600px; margin: auto; border-radius: 16px; overflow: hidden; border: 1px solid #e2e8f0;'>
                <div style='background-color: {SecondaryColor}; padding: 30px; text-align: center; color: white;'>
                    <h2 style='margin: 0;'>סיכום נסיעה וקבלה</h2>
                    <p style='opacity: 0.8;'>תודה שנסעת עם CityCar</p>
                </div>
                <div style='padding: 30px; background-color: white;'>
                    <p>שלום <b>{orderDetails.UserFullName ?? "לקוח יקר"}</b>,</p>
                    <p>להלן פירוט החיוב עבור נסיעה <b>#{orderDetails.Id}</b> ברכב <b>{orderDetails.CarModel}</b>:</p>
                    
                    <table style='width: 100%; border-collapse: collapse; margin: 20px 0;'>
                        <tr style='border-bottom: 1px solid #edf2f7;'>
                            <td style='padding: 12px 0; color: #64748b;'>מחיר בסיס ({orderDetails.PricingType})</td>
                            <td style='padding: 12px 0; text-align: left; font-weight: bold;'>₪{orderDetails.BasePrice:N2}</td>
                        </tr>";

            if (orderDetails.LateFee > 0)
            {
                message += $@"
                        <tr style='border-bottom: 1px solid #edf2f7; color: {DangerColor};'>
                            <td style='padding: 12px 0;'>קנס איחור בהחזרה</td>
                            <td style='padding: 12px 0; text-align: left; font-weight: bold;'>+ ₪{orderDetails.LateFee:N2}</td>
                        </tr>";
            }

            if (orderDetails.DiscountAmount > 0)
            {
                message += $@"
                        <tr style='border-bottom: 1px solid #edf2f7; color: {SuccessColor};'>
                            <td style='padding: 12px 0;'>הטבות וזיכויים</td>
                            <td style='padding: 12px 0; text-align: left; font-weight: bold;'>- ₪{orderDetails.DiscountAmount:N2}</td>
                        </tr>";
            }

            message += $@"
                        <tr>
                            <td style='padding: 20px 0; font-size: 20px; font-weight: 900; color: {SecondaryColor};'>סה""כ לחיוב</td>
                            <td style='padding: 20px 0; text-align: left; font-size: 20px; font-weight: 900; color: {PrimaryColor};'>₪{orderDetails.TotalPrice:N2}</td>
                        </tr>
                    </table>

                    <div style='background-color: #f1f5f9; padding: 15px; border-radius: 8px; font-size: 14px;'>
                        <p style='margin: 0;'><b>מרחק שבוצע:</b> {orderDetails.DistanceDrivenKm ?? 0} ק""מ</p>
                    </div>
                </div>
                <div style='background-color: #f8fafc; padding: 20px; text-align: center; color: #94a3b8; font-size: 12px;'>
                    נשמח לראותכם בנסיעה הבאה!
                </div>
            </div>";

            await SendEmailAsync(userEmail, $"קבלה נסיעה #{orderDetails.Id} - CityCar", message);
        }

        public async Task SendPasswordResetAsync(string userEmail, string resetCode)
        {
            string message = $@"
            <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 500px; margin: auto; text-align: center; padding: 40px; border: 1px solid #e2e8f0; border-radius: 16px;'>
                <h2 style='color: {SecondaryColor};'>איפוס סיסמה</h2>
                <p>קיבלנו בקשה לאיפוס הסיסמה שלך. קוד האימות שלך הוא:</p>
                <div style='background-color: #f1f5f9; display: inline-block; padding: 15px 40px; border-radius: 12px; font-size: 32px; font-weight: bold; color: {PrimaryColor}; letter-spacing: 5px; margin: 20px 0;'>
                    {resetCode}
                </div>
                <p style='color: {DangerColor}; font-size: 14px;'>הקוד תקף ל-5 דקות בלבד.</p>
            </div>";

            await SendEmailAsync(userEmail, "קוד לאיפוס סיסמה - CityCar", message);
        }

        public async Task SendWelcomeEmailAsync(string userEmail, string userName)
        {
            string message = $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, sans-serif; max-width: 600px; margin: auto; background-color: #ffffff; border-radius: 20px; overflow: hidden; border: 1px solid #e2e8f0;'>
                <div style='background: linear-gradient(135deg, {PrimaryColor} 0%, #4f46e5 100%); padding: 50px 20px; text-align: center;'>
                    <h1 style='color: white; margin: 0;'>ברוך הבא, {userName}!</h1>
                </div>
                <div style='padding: 40px; text-align: center;'>
                    <p style='font-size: 20px; color: {SecondaryColor};'>אנחנו נרגשים שהצטרפת למשפחת <b>CityCar</b>.</p>
                    <p style='color: #64748b; line-height: 1.8;'>מהיום, חוויית הנסיעה שלך משתדרגת. רכבים חדישים, זמינות מקסימלית ושירות ללא פשרות מחכים לך בכל פינה.</p>
                    <div style='margin-top: 30px; padding: 20px; border: 1px dashed #cbd5e1; border-radius: 12px;'>
                        <p style='margin: 0; color: {PrimaryColor}; font-weight: bold;'>""הדרך שלך חשובה לנו בדיוק כמו היעד שלך""</p>
                    </div>
                    <p style='margin-top: 40px; font-weight: bold;'>נסיעה טובה ובטוחה,<br>צוות CityCar</p>
                </div>
            </div>";

            await SendEmailAsync(userEmail, "ברוכים הבאים ל-CityCar! 🚗", message);
        }

        public async Task SendFineNotificationAsync(string userEmail, decimal amount, string reason)
        {
            string message = $@"
            <div dir='rtl' style='font-family: ""Segoe UI"", Tahoma, sans-serif; max-width: 600px; margin: auto; border-radius: 16px; overflow: hidden; border: 1px solid #e2e8f0;'>
                <div style='background-color: {DangerColor}; padding: 30px; text-align: center;'>
                    <h2 style='color: white; margin: 0;'>עדכון חשוב בנוגע לנסיעתך</h2>
                </div>
                <div style='padding: 30px; background-color: white;'>
                    <p style='font-size: 18px;'>שלום רב,</p>
                    <p>אנו מעדכנים כי חשבונך חויב בסך של <b>₪{amount:N2}</b>.</p>
                    <div style='background-color: #fff1f2; border-right: 4px solid {DangerColor}; padding: 20px; border-radius: 8px; margin: 20px 0;'>
                        <p style='margin: 0; color: #991b1b;'><b>סיבת החיוב:</b> {reason}</p>
                    </div>
                    <p style='font-size: 14px; color: #64748b; line-height: 1.6;'>
                        ב-CityCar אנו מחויבים לסטנדרט גבוה של ניקיון ותקינות עבור כל לקוחותינו. דיווחים אלו, המגיעים מהנהג שקיבל את הרכב מיד אחריך, עוזרים לנו לשמור על שירות איכותי לקהילה.
                    </p>
                    <hr style='border: 0; border-top: 1px solid #f1f5f9; margin: 30px 0;'>
                    <p style='font-size: 12px; color: #94a3b8; text-align: center;'>לבירורים או ערעור, ניתן לפנות לשירות הלקוחות בצירוף מספר הזמנה.</p>
                </div>
            </div>";

            await SendEmailAsync(userEmail, "עדכון על חיוב נוסף - CityCar", message);
        }

        public async Task SendRegistrationVerificationAsync(string userEmail, string verificationCode)
        {
            string message = $@"
            <div dir='rtl' style='font-family: Arial, sans-serif; max-width: 500px; margin: auto; text-align: center; padding: 40px; border: 1px solid #e2e8f0; border-radius: 16px;'>
                <h2 style='color: {SecondaryColor};'>אימות הרשמה</h2>
                <p>תודה שהצטרפת! קוד האימות שלך להשלמת הרישום הוא:</p>
                <div style='background-color: #f1f5f9; display: inline-block; padding: 15px 40px; border-radius: 12px; font-size: 32px; font-weight: bold; color: {PrimaryColor}; letter-spacing: 5px; margin: 20px 0;'>
                    {verificationCode}
                </div>
                <p style='color: #64748b;'>הקוד תקף ל-5 דקות בלבד.</p>
            </div>";

            await SendEmailAsync(userEmail, "קוד אימות להרשמה - CityCar", message);
        }
    }
}