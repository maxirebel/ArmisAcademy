using ArmisApp.Models.Domain.db;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Identity
{
    public class ApplicationUser :IdentityUser
    {
        public int? ImageID { get; set; }
        public int? SocialID { get; set; }

        [Display(Name = "نام")]
        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; }

        [Display(Name = "نام خانوادگی")]
        [MaxLength(100, ErrorMessage = "{0} نباید بیشتر از {1} باشد")]
        public string LastName { get; set; }

        [Display(Name = "آدرس آی پی")]
        public string IPAddress { get; set; }

        [Display(Name = "تاریخ ثبت نام")]
        public DateTime Date { get; set; }

        [Display(Name = "تاریخ تولد")]
        public DateTime Birth { get; set; }

        [Display(Name = "آخرین ورود")]
        public DateTime LastEntry { get; set; }

        public bool IsSignedIn { get; set; }

        [MaxLength(11)]
        [Required]
        public string Mobile { get; set; }

        public string MobileConfirmCode { get; set; }

        [Display(Name = "موجودی")]
        public int Inventory { get; set; }

        [Display(Name = "میزان بدهی")]
        public int DebtAmount { get; set; }

        [Display(Name = "امتیاز")]
        public int Score { get; set; }

        [Display(Name = "آدرس")]
        public string Address { get; set; }

        [Display(Name = "توضیحات")]
        public string Description { get; set; }

        [Display(Name = "کشور")]
        public string State { get; set; }

        [Display(Name = "استان")]
        public string Country { get; set; }

        [Display(Name = "شهر")]
        public string City { get; set; }

        [Display(Name = "کد پستی")]
        public string PostalCode { get; set; }

        [Display(Name = "کد ملی")]
        public string CodeMelli { get; set; }

        [Display(Name = "کد معرف")]
        public string ReagentCode { get; set; }

        // 0 : غیر فعال
        // 1 : فعال
        // 2 : مسدود
        public int Status { get; set; }

        [ForeignKey(nameof(ImageID))]
        public virtual TblImage TblImage { get; set; }

        [ForeignKey(nameof(SocialID))]
        public virtual TblUserSocial TblUserSocial { get; set; }
        public virtual ICollection<TblPlacement> TblPlacement { get; set; }
        public virtual ICollection<TblCourse> TblCourse { get; set; }
        public virtual ICollection<TblOnlineCourse> TblOnlineCourse { get; set; }
        public virtual ICollection<TblFiles> TblFiles { get; set; }
        public virtual ICollection<TblUserCourse> TblUserCourse { get; set; }
        public virtual ICollection<TblUserOnlineCourse> TblUserOnlineCourse { get; set; }
        public virtual ICollection<TblTransaction> TblTransaction { get; set; }
        public virtual ICollection<TblInvoice> TblInvoice { get; set; }
        public virtual ICollection<TblMessages> TblMessages { get; set; }
        public virtual ICollection<TblChat> TblChat { get; set; }
        public virtual ICollection<TblBank> TblBank { get; set; }
        public virtual ICollection<TblComment> TblComment { get; set; }
        public virtual ICollection<TblUserBoon> TblUserBoon { get; set; }
        public virtual ICollection<TblUserSession> TblUserSession { get; set; }
        public virtual ICollection<TblUserLevels> TblUserLevels { get; set; }
        public virtual ICollection<TblTicket> TblTicket { get; set; }
        public virtual ICollection<TblTicketMsg> TblTicketMsg { get; set; }
        public virtual ICollection<TblContact> TblContact { get; set; }
        public virtual ICollection<TblNews> TblNews { get; set; }
        public virtual ICollection<TblProduct> TblProduct { get; set; }
        public virtual ICollection<TblNewsComments> TblNewsComments { get; set; }
        public virtual ICollection<TblUserAccess> TblUserAccess { get; set; }
        public virtual ICollection<TblEarnTransaction> TblEarnTransaction { get; set; }
        public virtual ICollection<TblUserScore> TblUserScore { get; set; }
        public virtual ICollection<TblFriends> TblFriends { get; set; }
        public virtual ICollection<TblBlogReview> TblBlogReview { get; set; }
        public virtual ICollection<TblCourseReview> TblCourseReview { get; set; }
        public virtual ICollection<TblUserEarn> TblUserEarn { get; set; }
        public virtual ICollection<TblEarnReagent> TblEarnReagent { get; set; }
        public virtual ICollection<TblUserActivity> TblUserActivity { get; set; }
        public virtual ICollection<TblUserEvents> TblUserEvents { get; set; }
        public virtual ICollection<TblBookingInvoice> TblBookingInvoice { get; set; }
        public virtual ICollection<TblUserEventsInvoice> TblUserEventsInvoice { get; set; }
        public virtual ICollection<TblUserFiles> TblUserFiles { get; set; }


    }
}
