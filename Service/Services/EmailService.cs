using Common.Dto;
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
        // מימוש שליחת מייל בסיסי   
        public async Task SendEmailAsync(string toEmail, string subject, string htmlMessage)
        {
            var client = new SmtpClient("smtp.gmail.com", 587)
            {
                EnableSsl = true,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential("citydrive.system@gmail.com", "zfie iqgd vjmk jjnw")
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress("citydrive.system@gmail.com", "CityDrive System"),
                Subject = subject,
                Body = htmlMessage,
                IsBodyHtml = true // מאפשר לנו לשלוח מייל מעוצב
            };

            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }

        // מימוש שליחת אישור הזמנה
        public async Task SendOrderConfirmationAsync(string userEmail, int orderId, string carModel)
        {
            string message = $@"
                <h1>אישור הזמנה מספר {orderId}</h1>
                <p>הזמנתך לרכב מסוג {carModel} התקבלה בהצלחה.</p>
                <p>לפני תחילת הנסיעה, אנא מלא את שאלון מצב הרכב בקישור הבא:</p>
                <a href='https://citycar.co.il/survey?orderId={orderId}'>לחץ כאן למילוי השאלון</a>";

            await SendEmailAsync(userEmail, "אישור הזמנה - CityDrive", message);
        }

        public async Task SendFinalReceiptAsync(string userEmail, OrderDto orderDetails)
        {
            // עיצוב המייל בפורמט צנוע ומכובד
            string message = $@"
            <div style='direction: rtl; font-family: Arial, sans-serif; max-width: 600px; margin: auto; border: 1px solid #eeeeee; padding: 20px; border-radius: 8px;'>
            <h2 style='color: #2c3e50; text-align: center;'>סיכום נסיעה וקבלה - CityDrive</h2>
            <p>שלום <b>{orderDetails.UserFullName ?? "לקוח יקר"}</b>,</p>
            <p>תודה שנסעת עם CityDrive. להלן פירוט החיוב עבור הזמנה מספר {orderDetails.Id}:</p>
            
            <table style='width: 100%; border-collapse: collapse; margin-top: 15px;'>
            <thead>
                <tr style='background-color: #f2f2f2;'>
                    <th style='padding: 10px; border: 1px solid #dddddd; text-align: right;'>תיאור</th>
                    <th style='padding: 10px; border: 1px solid #dddddd; text-align: right;'>סכום</th>
                </tr>
            </thead>
            <tbody>
                <tr>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>מחיר נסיעה בסיסי ({orderDetails.PricingType})</td>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>₪{orderDetails.BasePrice:N2}</td>
                </tr>";
            
            // הצגת שורת קנס רק אם קיים חיוב כזה
            if (orderDetails.LateFee > 0)
            {
                message += $@"
                <tr style='color: #e74c3c;'>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>קנס איחור בהחזרה</td>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>+ ₪{orderDetails.LateFee:N2}</td>
                </tr>";
            }

            // הצגת שורת הנחה (קופון או תדלוק) רק אם קיימת
            if (orderDetails.DiscountAmount > 0)
            {
                message += $@"
                <tr style='color: #27ae60;'>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>הנחה / זיכוי תדלוק</td>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>- ₪{orderDetails.DiscountAmount:N2}</td>
                </tr>";
            }

            message += $@"
                <tr style='background-color: #f9f9f9; font-weight: bold;'>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>סה""כ לתשלום</td>
                    <td style='padding: 10px; border: 1px solid #dddddd;'>₪{orderDetails.TotalPrice:N2}</td>
                </tr>
               </tbody>
               </table>
              
               <div style='margin-top: 20px; font-size: 0.9em; color: #555555;'>
                   <p><b>פרטי נסיעה:</b><br>
                   רכב: {orderDetails.CarModel}<br>
                   מרחק נסיעה: {orderDetails.DistanceDrivenKm ?? 0} ק""מ</p>
               </div>
              
               <p style='text-align: center; margin-top: 30px; color: #7f8c8d;'>נשמח לראותכם שוב בדרכים!</p>
               </div>";

            await SendEmailAsync(userEmail, $"קבלה עבור נסיעה {orderDetails.Id} - CityDrive", message);
        }
        // שליחת קוד איפוס סיסמה
        public async Task SendPasswordResetAsync(string userEmail, string resetCode)
        {
            string message = $@"
        <div style='direction: rtl; font-family: Arial, sans-serif;'>
            <h2 style='color: #2c3e50;'>בקשה לאיפוס סיסמה</h2>
            <p>שלום רב,</p>
            <p>קיבלנו בקשה לאיפוס הסיסמה בחשבון ה-CityDrive שלך.</p>
            <p>הקוד האישי שלך הוא: <b style='font-size: 1.2em; color: #e74c3c;'>{resetCode}</b></p>
            <p style='color: #7f8c8d; font-size: 0.9em;'>שימי לב: הקוד תקף ל-<b>5 דקות בלבד</b>.</p>
            <p>אם לא ביקשת לאפס את הסיסמה, את יכולה להתעלם מהמייל הזה בבטחה.</p>
        </div>";

            await SendEmailAsync(userEmail, "קוד לאיפוס סיסמה - CityDrive", message);
        }

        // שליחת מייל ברוך הבא
        public async Task SendWelcomeEmailAsync(string userEmail, string userName)
        {
            string message = $@"
        <div style='direction: rtl; font-family: ""Segoe UI"", Tahoma, Geneva, Verdana, sans-serif; max-width: 600px; margin: auto; border: 1px solid #e0e0e0; padding: 30px; border-radius: 15px; text-align: center; background-color: #ffffff;'>
            <h1 style='color: #2c3e50; font-size: 24px;'>ברוך הבא למשפחת CityDrive, {userName}!</h1>
            
            <p style='font-size: 18px; color: #34495e; line-height: 1.6;'>
                אנחנו נרגשים ומלאי הערכה על כך שבחרת להצטרף אלינו. 
                בחירתך ב-<b>CityDrive</b> אינה מובנת מאליה, ואנו מחויבים להעניק לך את חוויית הנסיעה הטובה, הבטוחה והאיכותית ביותר שיש.
            </p>

            <div style='background-color: #f9f9f9; padding: 20px; border-radius: 10px; margin: 25px 0;'>
                <p style='margin: 0; color: #7f8c8d; font-style: italic;'>
                    ""הדרך שלך חשובה לנו בדיוק כמו היעד שלך.""
                </p>
            </div>

            <p style='font-size: 16px; color: #2c3e50;'>
                מהיום, הדרך שלך הופכת להרבה יותר פשוטה ומהנה. 
                אנחנו כאן לכל שאלה, ובטוחים שתיהנה מהרכבים ומהשירות הייחודי שלנו.
            </p>

            <p style='margin-top: 40px; font-weight: bold; color: #2c3e50;'>
                בברכה ובהערכה רבה,<br>
                צוות CityDrive
            </p>
            
            <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
            <small style='color: #bdc3c7;'>נשלח באהבה ממערכת ניהול הרכבים החכמה שלך</small>
        </div>";

            await SendEmailAsync(userEmail, $"ברוכים הבאים ל-CityDrive! אנחנו כל כך שמחים שאתה איתנו", message);
        }

        public async Task SendFineNotificationAsync(string userEmail, decimal amount, string reason)
        {
            string subject = "עדכון חשבון - סיטי-קאר";
            string body = $@"
        <div dir='rtl' style='font-family: Arial, sans-serif;'>
            <h2>שלום רב,</h2>
            <p>רצינו לעדכן כי חשבונך חויב בסך של <b>{amount} ש""ח</b>.</p>
            <p><b>סיבת החיוב:</b> {reason}</p>
            <p>אנו בסיטי-קאר עושים מאמץ גדול כדי שכל לקוח יקבל רכב נקי ומכובד. 
            דיווחים אלו עוזרים לנו לשמור על רמת שירות גבוהה לכלל הקהילה.</p>
            <hr>
            <p>לערעור או בירורים נוספים, ניתן לפנות למוקד השירות שלנו.</p>
            <p>בברכה,<br>צוות סיטי-קאר</p>
        </div>";

            await SendEmailAsync(userEmail, subject, body);
        }
    }
}
