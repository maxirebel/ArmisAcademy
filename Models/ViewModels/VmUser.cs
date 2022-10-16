using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmUser
    {
        public string ID { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string UserName { get; set; }

        public string Email { get; set; }

        public string IPAddress { get; set; }

        public string Date { get; set; }

        public string ProfileImage { get; set; }

        public string LastEntry { get; set; }

        public string Score { get; set; }
        public string Mobile { get; set; }
        public string PhoneNumber { get; set; }

        public int Inventory { get; set; }

        public string Birth { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public string State { get; set; }

        public string Country { get; set; }

        public string City { get; set; }

        public string PostalCode { get; set; }
        public string ReagentCode { get; set; }

        public string ShansnamehNumber { get; set; }

        public string CodeMelli { get; set; }

        public string Role { get; set; }

        public int Status { get; set; }
        public string CourseList { get; set; }
        public IFormFile Image { get; set; }

        public List<UserChats> LstChat { get; set; }
        public List<UserrTerms> LstTerms { get; set; }
        public UserSoical Social { get; set; }
        public UserFollow Follow { get; set; }

    }
    public class UserFollow
    {
        public bool IsFollowed { get; set; }
        public bool IsOnline { get; set; }
        public int FollowingCount { get; set; }
        public int FollowerCount { get; set; }
    }
    public class UsersCourse
    {
        public int ID { get; set; }

        public string UserID { get; set; }
        public string ProfileImage { get; set; }
        public string FullName { get; set; }
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string UserName { get; set; }

        public string Date { get; set; }

        public string LastEntry { get; set; }

        public string CourseTitle { get; set; }

        public int Level { get; set; }

        public string Degree { get; set; }

        public int Status { get; set; }
        public string LevelsList { get; set; }
    }
    public class UserChats
    {
        public int ID { get; set; }
        public string Date { get; set; }
        public string SenderName { get; set; }
        public string ProfileImage { get; set; }
    }
    public class UserSoical
    {
        public string TelegramID { get; set; }
        public string SkypeID { get; set; }
        public string InstagramID { get; set; }
        public string LinkedinID { get; set; }

    }
    public class UserrTerms
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public int Type { get; set; }
    }
}
