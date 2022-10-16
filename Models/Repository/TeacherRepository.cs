using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.ExMethod;
using ArmisApp.Models.Identity;
using ArmisApp.Models.Utility;
using ArmisApp.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.Repository
{
    public class TeacherRepository : IDisposable
    {
        private DataContext db = null;

        public TeacherRepository()
        {
            db = new DataContext();
        }
        public int GetCourseReviews(string UserID)
        {
            var q = db.TblCourseReview.Where(a => a.TblCourse.TeacherID == UserID).Count();
            return q;
        }
        public int GetTeacherCourseCount(string UserID)
        {
            var q = db.TblCourse.Where(a => a.TeacherID == UserID).Count();
            return q;
        }
        public int GetStudentCount(string UserID)
        {
            var q = db.TblUserCourse.Where(a => a.TblCourse.TeacherID == UserID)
                .Include(a=>a.TblCourse).Count();
            return q;
        }
        public List<VmNotic> getLastActivities(string UserID, int Take)
        {
            var qLevel=db.TblUserLevels.Where(a=>a.TblCourse.TeacherID==UserID)
                .Include(a => a.TblCourse)
                .Include(a => a.TblUser)
                .OrderByDescending(a => a.ID).ToList();

            List<VmNotic> lstNotic = new List<VmNotic>();
            TimeUtility time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qLevel)
            {
                VmNotic vm = new VmNotic();
                vm.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Title = vm.FullName+ " سطح "+item.Level +" از "+ item.TblCourse.Title +" را خریداری کرده است ";
                vm.Date = time.GetDateName(item.Date);
                vm.Time = item.Date.ToString("hh:mm");
                lstNotic.Add(vm);
            }

            return lstNotic;
        }
        public List<VmTicket> getLastUserTickets(string UserID,int Take)
        {
            var qTicket = db.TblTicket.Where(a=>a.Section==5).Where(A => A.RecivedID == UserID)
                .Include(a => a.TblUser).OrderByDescending(a=>a.Date)
                .Take(Take).ToList();

            List<VmTicket> lstTicket = new List<VmTicket>();
            TimeUtility time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qTicket)
            {
                VmTicket vm = new VmTicket();
                vm.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Status = item.Status;
                vm.Title = item.Title;
                vm.Link = "/Teacher/SupportDetails/" + item.ID;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd hh:mm");
                var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                lstTicket.Add(vm);
            }
            return lstTicket;
        }
        public List<SessionComments> getLastStudentComments(string UserID, int Count = 3)
        {
            //فقط نمایش دیدگاه های تایید شده
            var qComment = db.TblComment.Where(a => a.UserReciver == UserID)
                .Where(a => a.CourseID != null)
                .Where(a=>a.Status==1)
                .Include(a => a.TblUserSender)
                .Include(a => a.TblCourse)
                .OrderByDescending(a => a.Date)
                .Take(Count).ToList();
            List<SessionComments> vm = new List<SessionComments>();

            TimeUtility time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qComment)
            {
                SessionComments sc = new SessionComments();
                sc.ID = item.ID;
                sc.Text = item.Text.Length > 120 ? item.Text.Substring(0, 120) + " ..." : item.Text;
                sc.FullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                sc.Link = "/CoursePreview/" + item.TblCourse.ShortLink.Replace(" ", "-") + "/" + item.CourseID;
                sc.CourseName = item.TblCourse.Title;
                sc.Date = time.GetDateName(item.Date);
                var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                vm.Add(sc);
            }
            return vm;
        }
        public List<SessionComments> getLastStudentCommentsNotif(string UserID, int Count)
        {
            // فقط نمایش دیدگاه های تایید شده به استاد
            var qComment = db.TblComment
                .Where(a => a.UserReciver == UserID)
                .Where(a=>a.Status==1)
                .Include(a => a.TblUserSender)
                .Include(a => a.TblCourse)
                .OrderByDescending(a => a.Date)
                .Take(Count).ToList();
            List<SessionComments> vm = new List<SessionComments>();

            TimeUtility time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qComment)
            {
                SessionComments sc = new SessionComments();
                sc.ID = item.ID;
                sc.Text = item.Text;
                sc.FullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                sc.Link = "/CoursePreview/" + item.TblCourse.ShortLink.Replace(" ", "-") + "/" + item.CourseID;
                sc.Date = time.GetTimeName(item.Date);
                vm.Add(sc);
            }
            return vm;
        }
        public List<VmCourses> getLatestTeacherCourse(string UserID)
        {
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            List<VmCourses> lstCourse = new List<VmCourses>();
            var qCourse = db.TblCourse.Where(a=>a.TeacherID==UserID).Where(a => a.Status == 1).AsQueryable();

            qCourse.Include(a => a.TblLevelPrice).Load();
            qCourse = qCourse.OrderByDescending(a => a.Date).Take(3);

            foreach (var item in qCourse)
            {
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.ShortLink = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                if (item.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.ImageID);
                    vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                vm.Date = Time.GetTimeName(item.Date);
                vm.TotalPrice = "تعیین نشده";
                if (item.TblLevelPrice.Count() > 0)
                {
                    int TotalPrice = item.TblLevelPrice.Sum(a => a.Prise);
                    vm.TotalPrice = TotalPrice.ToString("N0").ConvertNumerals();
                    if (item.DiscountPercent > 0)
                    {
                        //محاسبه تخفیف بر اساس درصد
                        int DiscountPrice = ((item.DiscountPercent * (TotalPrice)) / 100).Value;
                        vm.Price = ((TotalPrice - DiscountPrice)).ToString("N0").ConvertNumerals() + " تومان";
                        vm.FinalPrice = ((TotalPrice - DiscountPrice));
                    }
                    else
                    {
                        vm.FinalPrice = TotalPrice;
                    }
                }
                lstCourse.Add(vm);
            }

            return lstCourse;
        }
        public List<VmOnlineCourse> getLatestTeacherOnlineClass(string UserID)
        {
            List<VmOnlineCourse> lstCourse = new List<VmOnlineCourse>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            PayRepository RepPay = new PayRepository();
            var qCourse = db.TblOnlineCourse.Where(a=>a.TeacherID==UserID).Where(a => a.Status == 1).AsQueryable();

            qCourse.Include(a => a.TblOnlineCoursePrice).Load();
            qCourse = qCourse.OrderByDescending(a => a.Date).Take(3);
            foreach (var item in qCourse)
            {
                VmOnlineCourse vm = new VmOnlineCourse();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Status = item.Status;
                vm.Date = Time.GetTimeName(item.Date);
                vm.Type = item.Type;
                vm.TotalPrice = item.TotalPrice;
                vm.SessionsCount = item.SessionsCount;
                vm.Link = "/ClassView/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                if (item.Type == 2)
                {
                    if (item.TblOnlineCoursePrice != null)
                    {
                        vm.TotalPrice = item.TblOnlineCoursePrice.Price;
                    }
                    RepPay.GetPackageViewPrice(item.TblOnlineCoursePrice, vm);
                }
                if (item.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.ImageID);
                    vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                else
                {
                    vm.Image = "/assets/media/misc/image2.png";
                }
                lstCourse.Add(vm);
            }
            return lstCourse;
        }


        public int GetUnReadTickets(string UserID)
        {
            var q = db.TblTicket.Where(a => a.Status == 0).Where(a => a.UserID == UserID).Count();
            return q;
        }
        ~TeacherRepository()
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
