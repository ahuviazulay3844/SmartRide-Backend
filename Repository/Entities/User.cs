using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Repository.Entities
{
    public enum UserType { user, admin }
    public enum UserRank { Regular, Bronze, Silver, Gold, PurpleBadge }

    public class User
    {


        // --- Personal information and access ---

        [Key]
        public int Id { get; set; } 
        [Required]
        public string PasswordHash { get; set; } 
        [Required, MaxLength(50)]
        public string FirstName { get; set; } 
        [MaxLength(50)]
        public string? LastName { get; set; } 
        [Required, EmailAddress]
        public string Email { get; set; } 
        [Required, Phone]
        public string PhoneNumber { get; set; } 
        [Required]
        public DateTime DateOfBirth { get; set; } 
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public UserType UserType { get; set; }

        public string Address { get; set; }

        // --- Driver's license data ---

        [MaxLength(255)]
        public string? LicenseNumber { get; set; } 

        public DateTime? LicenseExpirationDate { get; set; }
        public bool IsLicenseVerified { get; set; } = false;                                                 
        public string? LicenseFrontImg { get; set; }
        public string? LicenseBackImg { get; set; }
        public string? SelfieImg { get; set; }
        public bool IsNewDriver { get; set; } = false;


        // --- Fields for an overseas citizen ---
        public bool IsForeignCitizen { get; set; } = false; 
        [MaxLength(255)]
        public string? PassportNumber { get; set; }
        public string? PassportImg { get; set; }   
        public string? VisaImg { get; set; }      
        public string? EntryPermitImg { get; set; } 
        public string? CountryOfOrigin { get; set; }

        // ---Encrypted payment details  ---
        [MaxLength(255)]
        public string? CardNumber { get; set; } 
        [MaxLength(255)]
        public string? CardExpiry { get; set; } 
        [MaxLength(255)]
        public string? CVV { get; set; }




        // ---User level---
        public int CompletedOrdersCount { get; set; } = 0; 
        public UserRank Rank { get; set; } = UserRank.Regular;



        //---Financial status and reliability---
        public decimal AccountBalance { get; set; } = 0; 
        public int DirtyReportsCount { get; set; } = 0; 
        public bool IsBlocked { get; set; } = false;

        // --- Password recovery fields ---
        public string? PasswordResetCode { get; set; } 
        public DateTime? ResetCodeExpiration { get; set; } 


        public virtual ICollection<Order> Orders { get; set; } = new List<Order>(); 
        public virtual ICollection<Coupon> Coupons { get; set; } = new List<Coupon>();  
    }
}
