using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.ExMethod;
using ArmisApp.Models.Utility;
using ArmisApp.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Repository
{
    public class UserRepository : IDisposable
    {
        private DataContext db = null;
        public bool CheckVideoToken(string ID,string token)
        {
            var qVid = db.TblVideo.Where(a => a.Token == token)
                .Where(a=>a.ID==Convert.ToInt32(ID))
                .FirstOrDefault();
            if (qVid != null)
            {
                return true;
            }
            return false;
        }
        public UserRepository()
        {
            db = new DataContext();
        }
        public int getTicketCount(string UserID)
        {
            var q = db.TblTicket.Where(a=>a.UserID==UserID).Count();
            return q;
        }
        public int getUserCommentCount(string UserID)
        {
            var q = db.TblComment.Where(a => a.CourseID != null).Where(a => a.UserSender == UserID).Count();
            return q;
        }
        public int getInventory(string UserID)
        {
            var q = db.Users.Where(a => a.Id == UserID).SingleOrDefault().Inventory;
            return q;
        }
        public int getSubmittedCourseCount(string UserID)
        {
            var q = db.TblUserCourse.Where(a => a.UserID == UserID).Count();
            return q;
        }
        public int GetNewUserOfDay()
        {
            var q = db.Users.Where(a=>a.Date>DateTime.Now.AddDays(-1)).ToList().Count();
            return q;
        }
        public int GetNewUserOfWeek()
        {
            var q = db.Users.Where(a => a.Date > DateTime.Now.AddDays(-7)).ToList().Count();
            return q;
        }
        public int GetNewUserOfMonth()
        {
            var q = db.Users.Where(a => a.Date > DateTime.Now.AddDays(-30)).ToList().Count();
            return q;
        }
        public int GetUsersCount()
        {
            var q = db.Users.ToList().Count();
            return q;
        }
        public int GetCourseCount()
        {
            var q = db.TblCourse.ToList().Count();
            return q;
        }
        public int GetPaymentOfDay()
        {
            var q = db.TblTransaction
                .Where(a=>a.Type==1 ||a.Type==2 || a.Type == 5)
                .Where(a=>a.Date > DateTime.Now.AddDays(-1)).ToList().Count();
            return q;
        }
        public int GetPaymentOfDayAmount()
        {
            var q = db.TblTransaction
                .Where(a => a.Type == 1 || a.Type == 2 || a.Type == 5)
                .Where(a => a.Date > DateTime.Now.AddDays(-1)).Sum(a=>a.Amount);
            return q;
        }
        public int GetPaymentOfMonth()
        {
            var q = db.TblTransaction
                .Where(a => a.Type == 1 || a.Type == 2 || a.Type == 5)
                .Where(a => a.Date > DateTime.Now.AddDays(-30)).ToList().Count();
            return q;
        }
        public int GetPaymentOfMonthAmount()
        {
            var q = db.TblTransaction
                .Where(a => a.Type == 1 || a.Type == 2 || a.Type == 5)
                .Where(a => a.Date > DateTime.Now.AddDays(-30)).Sum(a => a.Amount);
            return q;
        }
        public int GetPaymentOfYear()
        {
            var q = db.TblTransaction
                .Where(a => a.Type == 1 || a.Type == 2 || a.Type == 5)
                .Where(a => a.Date > DateTime.Now.AddYears(-1)).ToList().Count();
            return q;
        }
        public int GetPaymentOfYearAmount()
        {
            var q = db.TblTransaction
                .Where(a => a.Type == 1 || a.Type == 2 || a.Type == 5)
                .Where(a => a.Date > DateTime.Now.AddYears(-1)).Sum(a => a.Amount);
            return q;
        }
        public List<VmTicket> getLastUserTickets(string UserID)
        {
            var qTicket = db.TblTicket.Where(A => A.UserID == UserID).Include(a => a.TblUser).Take(7).ToList();

            List<VmTicket> lstTicket = new List<VmTicket>();
            TimeUtility time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qTicket)
            {
                VmTicket vm = new VmTicket();
                vm.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Status = item.Status;
                vm.Title = item.Title;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd hh:mm");
                var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                lstTicket.Add(vm);
            }
            return lstTicket;
        }
        public VmUser GetUserDetails(string UserID,string roleName)
        {
            var qUser = db.Users.Where(a => a.Id == UserID).SingleOrDefault();
            FileRepository RepImg = new FileRepository();
            VmUser vm = new VmUser();
            switch (roleName)
            {
                case "Student":
                    vm.Role = "هنرجو";
                    break;
                case "Admin":
                    vm.Role = "مدیر";
                    break;
                case "Teacher":
                    vm.Role = "استاد";
                    break;
            }
            vm.FullName = qUser.FirstName+" "+ qUser.LastName;
            vm.Email = qUser.Email;
            var Image = RepImg.GetImageByID(qUser.ImageID);
            vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
            
            return vm;
        }

        public List<VmUser> GetStudents(int Sort, string UserID)
        {
            var qCourse = db.TblCourse.Where(a => a.TeacherID == UserID).ToList();

            List<VmUser> lstUser = new List<VmUser>();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qCourse)
            {
                var qStud = db.TblUserCourse.Where(a => a.CourseID == item.ID).Include(a=>a.TblUser).AsQueryable();
                if (Sort == 1)
                {
                    qStud = qStud.Where(a => a.Date > DateTime.Now.AddDays(-1));
                }
                //بر اساس ماه
                if (Sort == 3)
                {
                    qStud = qStud.Where(a => a.Date > DateTime.Now.AddDays(-30));
                }
                foreach (var itemStud in qStud.ToList())
                {
                    VmUser vm = new VmUser();
                    vm.FirstName = itemStud.TblUser.FirstName;
                    vm.LastName = itemStud.TblUser.LastName;
                    vm.Email = itemStud.TblUser.Email;

                    var Image = RepImg.GetImageByID(itemStud.TblUser.ImageID);
                    vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    lstUser.Add(vm);
                }
            }
            return lstUser;
        }
        public List<SessionComments> getLastSendedComments(string UserID,int Count=3)
        {
            var qComment = db.TblComment.Where(a=>a.CourseID!=null).Where(a => a.UserSender == UserID)
                .Include(a=>a.TblUserSender)
                .Include(a=>a.TblCourse)
                .OrderByDescending(a=>a.Date)
                .Take(Count).ToList();
            List<SessionComments> vm = new List<SessionComments>();

            TimeUtility time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qComment)
            {
                SessionComments sc = new SessionComments();
                sc.ID = item.ID;
                sc.Text = item.Text;
                sc.Link = "/Course/" + item.TblCourse.ShortLink;
                sc.Date = time.GetDateName(item.Date);
                sc.CourseName = item.TblCourse.Title;
                var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                vm.Add(sc);
            }
            return vm;
        }
        public List<VmMessage> GetLastNotics(string UserID, int Count)
        {
            var qFollow = db.TblFriends.Where(a => a.UserReceiver == UserID)
                .Include(a => a.TblUser).ThenInclude(a=>a.TblImage).ThenInclude(a=>a.TblServer);

            List<VmMessage> LstNotic = new List<VmMessage>();
            FileRepository RepImg = new FileRepository();
            TimeUtility time = new TimeUtility();
            foreach (var item in qFollow)
            {
                VmMessage vm = new VmMessage();
                vm.ID = item.ID;
                vm.Read = item.Read;
                vm.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Text = vm.SenderName + " شما را دنبال می کند";
                vm.Link = "/Notic/follow/" + item.ID;
                vm.DateName = time.GetTimeName(item.Date);
                var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                LstNotic.Add(vm);
            }
            var Notics = LstNotic.AsQueryable()
                .OrderByDescending(a => a.Read == false)
                .OrderByDescending(a => a.Date)
                .Take(Count).ToList();
            return Notics;
        }

        public List<VmMessage> GetLastMessages(string UserID,int Count,bool Admin)
        {
            var qNotic = db.TblMessages.Include(a=>a.TblCourse).Include(a => a.TblUser).AsQueryable();
            var qMsgTicket = db.TblTicketMsg.Include(a => a.TblUserSender).AsQueryable();
            if (!Admin)
            {
                qNotic = qNotic.Where(a => a.ReaseverID == UserID).Where(a => a.CourseID != null);
                qMsgTicket = qMsgTicket.Where(a => a.ReceiverID == UserID).Where(a=>a.TblTicket.Section!=5);
            }

            string RoleID = db.UserRoles.Where(a => a.UserId == UserID).FirstOrDefault().RoleId;
            string RoleName = db.Roles.Where(a => a.Id == RoleID).SingleOrDefault().Name;

            List<VmMessage> LstNotic = new List<VmMessage>();
            FileRepository RepImg = new FileRepository();
            TimeUtility time = new TimeUtility();
            foreach (var item in qNotic.ToList())
            {
                VmMessage vm = new VmMessage();
                vm.ID = item.ID;
                vm.Type = "conv";
                vm.Read = item.Read;
                vm.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Text = vm.SenderName+" پیام جدیدی را ارسال کرده است";
                vm.Link = "/CoursePreview/" + (item.TblCourse.ShortLink != null ? item.TblCourse.ShortLink.Replace(" ", "-") : item.TblCourse.Title.Replace(" ", "-")) + "/" + item.CourseID + "#kt_tab_pane_2_4";
                vm.Date = time.GetDateName(item.Date);
                vm.DateName = time.GetTimeName(item.Date);
                vm.Time = item.Date.ToString("hh:mm");
                var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                LstNotic.Add(vm);
            }
            foreach (var item in qMsgTicket)
            {
                VmMessage vm = new VmMessage();
                vm.ID = item.ID;
                vm.Type = "ticket";
                vm.Read = item.Read;
                vm.SenderName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                vm.Text = vm.SenderName+" پیام جدیدی ارسال کرده است";
                switch (RoleName)
                {
                    case "Admin":
                        vm.Link = "/Admin/SupportDetails/" + item.TicketID;
                        break;
                    case "Student":
                        vm.Link = "/Profile/SupportDetails/" + item.TicketID;
                        break;
                    case "Teacher":
                        vm.Link = "/Profile/SupportDetails/" + item.TicketID;
                        break;
                }
                vm.Date = time.GetDateName(item.Date);
                vm.DateName = time.GetTimeName(item.Date);
                vm.Time = item.Date.ToString("hh:mm");
                var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                LstNotic.Add(vm);
            }
            var Notics = LstNotic.AsQueryable()
                .OrderByDescending(a => a.Date)
                .OrderByDescending(a => a.Read == false).Take(Count).ToList();
            return Notics;
        }
        public int GetNoticsCount(string UserID)
        {
            int qNotic = db.TblMessages.Where(a => a.ReaseverID == UserID)
                .Where(a => a.Read == false).Count();

            int qMsgTicket = db.TblTicketMsg.Where(a => a.ReceiverID == UserID)
                .Where(a => a.Read == false).Count();

            int qFollow = db.TblFriends.Where(a => a.UserReceiver == UserID)
                .Where(a => a.Read == false).Count();

            int TotalNotic = qNotic + qMsgTicket + qFollow;

            return TotalNotic;
        }
        public int getProfileProgres(string UserID)
        {
            var User = db.Users.Where(a => a.Id == UserID).SingleOrDefault();
            int Number = 0;
            int Count = 18;
            if (User.Mobile != null)
            {
                Number += 2;
            }
            if (User.Birth != null)
            {
                Number += 1;
            }
            if (User.PhoneNumber != null)
            {
                Number += 1;
            }
            if (User.Address != null)
            {
                Number += 2;
            }
            if (User.CodeMelli != null)
            {
                Number += 1;
            }

            if (User.State != null && User.City!=null)
            {
                Number += 2;
            }
            if (User.Description != null)
            {
                Number += 2;
            }
            if (User.FirstName != null && User.LastName != null)
            {
                Number += 4;
            }
            if (User.PostalCode != null)
            {
                Number += 1;
            }
            if (User.SocialID != null)
            {
                Number += 2;
            }
            //if (User.TblBank != null)
            //{
            //    Number += 2;
            //}
            int Percent = (Number * 100) / Count;
            return Percent;
        }
        public int GetUnReadConversation(string UserID)
        {
            var q = db.TblMessages.Where(a => a.ReaseverID == UserID)
                .Where(a=>a.SenderID!=UserID)
                .Where(a => a.Read == false)
                .Count();
            return q;
        }
        public VmUser GetProfileDetails(string UserID,string roleName)
        {
            var user = db.Users.Where(a => a.Id == UserID).SingleOrDefault();
            VmUser vm = new VmUser();
            FileRepository RepImg = new FileRepository();
            UserRepository RepUser = new UserRepository();
            TimeUtility Time = new TimeUtility();

            vm.ID = user.Id;
            vm.FullName = user.FirstName + " " + user.LastName;

            switch (roleName)
            {
                case "Student":
                    vm.Role = "هنرجو";
                    break;
                case "Admin":
                    vm.Role = "مدیر";
                    break;
                case "Teacher":
                    vm.Role = "استاد";
                    break;
            }
            var Image = RepImg.GetImageByID(user.ImageID);
            vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
            #region UserInfo
            vm.Score = user.Score.ToString();
            vm.LastEntry = Time.GetTimeName(user.LastEntry);
            vm.Follow = new UserFollow();
            var qFollow = db.TblFriends;
            if (UserID != user.Id)
            {
                var qFollowed = qFollow.Where(a => a.UserSender == UserID).Where(a => a.UserReceiver == user.Id).SingleOrDefault();
                if (qFollowed != null)
                {
                    vm.Follow.IsFollowed = true;
                }
            }
            if (user.IsSignedIn && user.LastEntry.AddMinutes(30) > DateTime.Now)
            {
                vm.Follow.IsOnline = true;
            }
            vm.Follow.FollowingCount = qFollow.Where(a => a.UserSender == user.Id).Count();
            vm.Follow.FollowerCount = qFollow.Where(a => a.UserReceiver == user.Id).Count();
            #endregion
            return vm;
        }
        public string GetClientIP(HttpContext context)
        {
            var ipAddress = (string)context.Connection.RemoteIpAddress?.ToString();
            return ipAddress;
        }
        ~UserRepository()
        {
            Dispose(true);
        }

        public void Dispose()
        {

        }

        public void Dispose(bool Dis)
        {
            if (Dis)
            {
                Dispose();
            }
        }
    }

}
