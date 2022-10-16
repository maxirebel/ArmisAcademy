using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using ArmisApp.Models.Domain.context;
using Microsoft.EntityFrameworkCore;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.Identity;
using Microsoft.AspNetCore.Identity;
using ArmisApp.Models.ViewModels;
using ArmisApp.Models.ExMethod;
using Microsoft.AspNetCore.Mvc.Rendering;
using ArmisApp.Models.Repository;
using ArmisApp.Models.Utility;
using System.IO;
using Microsoft.AspNetCore.Http;
using System.Text.Encodings.Web;
using ArmisApp.Services;
using Microsoft.Extensions.Hosting;

namespace ArmisApp.Controllers
{
    [Authorize]
    public class StudentController : Controller
    {
        private readonly IHostEnvironment _appEnvironment;
        private readonly DataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;

        public StudentController(
            DataContext context,
            IEmailSender emailSender,
            IHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
            _appEnvironment = environment;
            _emailSender = emailSender;

        }
        public IActionResult Index()
        {
            if (User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Admin");
            }
            if (User.IsInRole("Teacher"))
            {
                return RedirectToAction("Index", "Teacher");
            }
            UserRepository RepUser = new UserRepository();

            ViewBag.ProfileProgress = RepUser.getProfileProgres(User.GetUserID());
            ViewData["PageTitle"] = "خانه";
            ViewData["currentTab"] = "home";
            ViewData["current_subTab"] = "home";
            return View();
        }
        [HttpPost]
        public IActionResult SelectCourse(int CourseID)
        {
            try
            {
                string userID = User.GetUserID();
                TblUserCourse t = new TblUserCourse();
                t.CourseID = CourseID;
                t.UserID = userID;
                t.Date = DateTime.Now;
                _context.Add(t);
                _context.SaveChanges();

                return Json(new { result = "ok", msg = "دوره مورد نظر با موفقیت اضافه گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطای غیر منتظره ! لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        [Route("Profile/StudentCourses")]
        public IActionResult StudentCourses()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["Courses"] = new SelectList(_context.TblCourse.Where(a => a.TeacherID == UserID), "Title", "Title");
            ViewData["currentNav"] = "courses";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "دوره های من";
            return View();
        }
        public JsonResult GetCourses()
        {
            string UserID = User.GetUserID();
            var qCourse = _context.TblUserCourse.Where(a => a.UserID == UserID)
                .Include(t => t.TblCourse).ThenInclude(a => a.TblGroup)
                .Include(a => a.TblCourse).ThenInclude(a => a.TblUser)
                .Include(t => t.TblUser).ThenInclude(t => t.TblImage)
                .OrderByDescending(a => a.Date).AsQueryable();

            List<VmCourses> lstCourse = new List<VmCourses>();
            FileRepository RepImg = new FileRepository();
            foreach (var itemSub in qCourse)
            {
                var item = itemSub.TblCourse;
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Status = item.Status;
                vm.Link = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.Date = itemSub.Date.ToShamsi().ToString("yyyy/MM/dd");
                var groupID = item.TblGroup.GroupID;
                vm.GroupName = _context.TblGroup.Where(a => a.ID == groupID).FirstOrDefault().Title;
                vm.SubGroupName = item.TblGroup.Title;
                vm.PercentSale = item.PercentSale;
                if (item.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.ImageID);
                    vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                lstCourse.Add(vm);
            }
            return Json(lstCourse);
        }
        [Route("Profile/MyOnlineClass")]
        public IActionResult MyOnlineClass()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var qClass = _context.TblUserOnlineCourse.Where(a => a.UserID == UserID)
                .Include(a => a.TblOnlineCourse).ThenInclude(a=>a.TblUser)
                .Include(a=>a.TblOnlineCourse)
                .OrderByDescending(a => a.Date);

            var qUserEvents = _context.TblUserEvents;

            FileRepository RepImage = new FileRepository();
            PayRepository RepPay = new PayRepository();
            //List<VmOnlineCourse> LstVm = new List<VmOnlineCourse>();
            List<VmClassStudent> LstVm = new List<VmClassStudent>();

            foreach (var item in qClass)
            {
                VmClassStudent vm = new VmClassStudent();
                vm.ID = item.ID;
                vm.Title = item.TblOnlineCourse.Title;
                vm.Teacher = item.TblOnlineCourse.TblUser.FirstName + " " + item.TblOnlineCourse.TblUser.LastName;
                vm.Type = item.TblOnlineCourse.Type;
                vm.Link = "/ClassView/" + (item.TblOnlineCourse.ShortLink != null ? item.TblOnlineCourse.ShortLink.Replace(" ", "-") : item.TblOnlineCourse.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd");
                vm.Status = item.Status;
                if (item.EndDate.Year == 0001)
                {
                    DateTime finishDate = item.Date.AddDays(45);
                    item.EndDate = finishDate;
                    _context.Update(item);
                }
                DateTime nowDate = DateTime.Today;
                int remainingDay = (item.EndDate - nowDate).Days;
                vm.RemainingDay = remainingDay <= 0 ? 0 : remainingDay;

                if (item.TblOnlineCourse.Type == 2)
                {
                    int checkEvents = qUserEvents.Where(a => a.InvoiceID == item.InvoiceID).Where(a => a.Status == 0 || a.Status == 2).Count();
                    int totalEvent = qUserEvents.Where(a => a.InvoiceID == item.InvoiceID).Count();

                    if (vm.RemainingDay > 0  && checkEvents > 0 && item.Status<2)
                    {
                        vm.CancelEnable = true;
                    }
                }
                vm.Package = RepPay.GetPackageName(item.Package);
                var cImage = RepImage.GetImageByID(item.TblOnlineCourse.ImageID);
                if (cImage != null)
                {
                    vm.Image = cImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + cImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + cImage.FileName;
                }
                else
                {
                    vm.Image = "/assets/media/misc/image2.png";
                }
                LstVm.Add(vm);
            }
            _context.SaveChanges();

            ViewData["currentNav"] = "myOnline";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View(LstVm);
        }
        [Route("Profile/MyClassScheduling/{ID}")]
        public IActionResult MyClassScheduling(int ID)
        {
            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var qUserClass = _context.TblUserOnlineCourse.Where(a => a.UserID == UserID).Where(a => a.ID == ID).FirstOrDefault();
            var qUserEvents = _context.TblUserEvents.Where(a => a.InvoiceID == qUserClass.InvoiceID).ToList();

            int UserEventCount= qUserEvents.Count();

            //DateTime finishDate = qUserClass.Date.AddDays(45);
            //DateTime nowDate = DateTime.Today;
            //int remainingDay = (finishDate - nowDate).Days;

            VmSchedule vm = new VmSchedule();
            vm.CourseID = qUserClass.CourseID;
            vm.InvoiceID = qUserClass.InvoiceID;
            vm.ClassNum = qUserClass.Package;

            DateTime nowDate = DateTime.Today;
            int remainingDay = (qUserClass.EndDate - nowDate).Days;
            vm.RemainingDay = remainingDay <= 0 ? 0 : remainingDay;
            vm.RemainingClassNum = vm.ClassNum - UserEventCount;

            if (vm.RemainingDay == 0)
            {
                qUserClass.Status = 2;
                _context.Update(qUserClass);
                _context.SaveChanges();
            }

            vm.lstReservedClass = new List<VmReservedClass>();
            foreach (var item in qUserEvents.Where(a => a.Status <= 3))
            {
                VmReservedClass vc = new VmReservedClass();
                vc.ID = item.ID;
                vc.Title = "جلسه رزرو شده شده در تاریخ " + item.StartDate.ToShamsi().ToShortDateString() + " در ساعت " + item.StartDate.ToShortTimeString();
                vc.Status = item.Status;
                vm.lstReservedClass.Add(vc);
            }

            vm.lstCanceledClass = new List<VmCanceledClass>();
            foreach (var item in qUserEvents.Where(a=>a.Status==2))
            {
                VmCanceledClass vc = new VmCanceledClass();
                vc.ID = item.ID;
                vc.Title = "جلسه لغو شده در تاریخ "+item.StartDate.ToShamsi().ToShortDateString()+" در ساعت "+item.StartDate.ToShortTimeString();
                vm.lstCanceledClass.Add(vc);
            }

            ViewBag.UserClassID = ID;
            ViewData["currentNav"] = "myOnline";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View(vm);
        }
        [HttpPost]
        public JsonResult MyClassScheduling([FromBody] VmSchedule data)
        {
            if (data.lstEvents != null)
            {
                foreach (var item in data.lstEvents)
                {
                    if (item.id == null &&item.groupId == null)
                    {
                        TblUserEvents t = new TblUserEvents();
                        t.Enable = true;
                        t.InvoiceID = data.InvoiceID;
                        t.CourseID = data.CourseID;
                        t.UserID = User.GetUserID();
                        t.StartDate = item.start;
                        t.EndDate = item.end;
                        t.Title = item.title;
                        _context.Add(t);
                    }
                }
                _context.SaveChanges();
            }
            return Json("ok");
        }
        [Route("Profile/ResetClassScheduling/{ID}/{ueID}")]
        public IActionResult ResetClassScheduling(int ID, int ueID)
        {
            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var qUserClass = _context.TblUserOnlineCourse.Where(a => a.UserID == UserID).Where(a => a.ID == ID).FirstOrDefault();
            var qUserEvents = _context.TblUserEvents.Where(a => a.ID == ueID).ToList();

            if(qUserEvents.FirstOrDefault() ==null || qUserEvents.FirstOrDefault().Status != 2)
            {
                return RedirectToAction(nameof(MyClassScheduling),new { ID = qUserClass.ID });
            }

            VmSchedule vm = new VmSchedule();
            vm.CourseID = qUserClass.CourseID;
            vm.InvoiceID = qUserClass.InvoiceID;

            vm.lstCanceledClass = new List<VmCanceledClass>();
            foreach (var item in qUserEvents.Where(a => a.Status == 2))
            {
                VmCanceledClass vc = new VmCanceledClass();
                vc.ID = item.ID;
                vc.Title = "کلاس لغو شده در تاریخ " + item.StartDate.ToShamsi().ToShortDateString() + " در ساعت " + item.StartDate.ToShortTimeString();
                vm.lstCanceledClass.Add(vc);
            }

            ViewBag.UserClassID = ID;
            ViewBag.UserEventID = ueID;
            ViewData["currentNav"] = "myOnline";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View(vm);
        }
        [HttpPost]
        public JsonResult ResetClassScheduling([FromBody] VmSchedule data)
        {
            if (data.lstEvents != null)
            {
                foreach (var item in data.lstEvents.Where(a=>a.groupId == "resetCancel"))
                {
                    if (item.id != null)
                    {
                        var qUserEvent= _context.TblUserEvents.Where(a => a.ID == Convert.ToInt32(item.id)).FirstOrDefault();
                        qUserEvent.StartDate = item.start;
                        qUserEvent.EndDate= item.end;
                        qUserEvent.Status = 0;
                        _context.Update(qUserEvent);
                    }
                }
                _context.SaveChanges();
            }
            return Json("ok");
        }
        [HttpGet]
        public JsonResult getEventData(int courseID,int invoiceID)
        {
            string UserID = User.GetUserID();
            var qEvents = _context.TblOnlineScheduling.Where(a => a.CourseID == courseID).ToList();

            var qUserEvents = _context.TblUserEvents.Where(a => a.CourseID == courseID);

            var qMyEvents = qUserEvents.Where(a => a.UserID == UserID).Where(a=>a.InvoiceID== invoiceID).ToList();
            var qReservedEvents = qUserEvents.Where(a => a.InvoiceID != invoiceID).ToList();

            bool update = false;

            List<VmScheduleEvent> LstVm = new List<VmScheduleEvent>();
            foreach (var item in qEvents)
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = item.Title;
                vm.groupId = "availableForClass";
                vm.display = "background";
                LstVm.Add(vm);
            }
            foreach (var item in qReservedEvents)
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = "رزرو شده";
                vm.overlap = false;
                vm.groupId = "reservedForClass";
                vm.constraint = "reservedForClass";
                vm.color = "grey";
                LstVm.Add(vm);
            }
            foreach (var item in qMyEvents)
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = item.Title;

                DateTime end15Minutes = item.EndDate.AddMinutes(15);
                if (DateTime.Now > end15Minutes && item.Status ==0)
                {
                    item.Status = 1;
                    update = true;
                    _context.Update(item);
                }
                if (item.Status == 1)
                {
                    vm.title = "اتمام";
                    vm.color = "green";
                }
                if (item.Status == 2)
                {
                    vm.title = "لغو";
                    vm.color = "orange";
                }
                if (item.Status == 3)
                {
                    vm.title = "لغو";
                    vm.color = "red";
                    vm.groupId = "cancelClass";

                }
                LstVm.Add(vm);
            }
            if (update)
            {
                _context.SaveChanges();
            }
            return Json(LstVm.ToArray());
        }
        [HttpPost]
        public async Task<JsonResult> cancelEventData(int ID)
        {
            // لغو جلسه توسط هنرجو
            try
            {
                var qUserEvents = _context.TblUserEvents.Where(a => a.ID == ID)
                    .Include(a=>a.TblUserEventsInvoice)
                    .Include(a => a.TblOnlineCourse).ThenInclude(a => a.TblOnlineCoursePrice).SingleOrDefault();

                var qUserClass = _context.TblUserOnlineCourse.Where(a => a.InvoiceID == qUserEvents.InvoiceID)
                    .Include(a => a.TblBookingInvoice).SingleOrDefault();
                if (qUserEvents.Status > 0 || qUserEvents.StartDate < DateTime.Now)
                {
                    return Json(new { result = "error", msg = "امکان حذف کلاس های برگزار شده را ندارید" }) ;
                }
                int dur4 = 4;
                int dur17 = 17;
                int percent = 0;
                int totalPrice = qUserClass.TblBookingInvoice.Amount;
                int tPercent = qUserClass.TblBookingInvoice.TeacherPercent > 0 ? qUserClass.TblBookingInvoice.TeacherPercent : qUserEvents.TblOnlineCourse.TeacherPercent;

                // حق الزحمه سایت = مبلغ کل - دستمزد استاد
                //int WagePrice = totalPrice - ((totalPrice * tPercent) / 100);

                int basePrice = (totalPrice)/ qUserClass.Package;
                int percentAmount = 0;
                int teacherAmount = 0;
                int siteAmount = 0;
                int payToStudentAmount = 0;
                int teacherfinalAmount = 0;

                //basePrice = basePrice - ((basePrice * tPercent) / 100);
                if (qUserEvents.StartDate <= DateTime.Now.AddHours(dur17))
                {
                    if (qUserEvents.StartDate <= DateTime.Now.AddHours(dur4))
                    {
                        // لغو در بازه 4 ساعته
                        // کسر 30 درصد
                        percent = 30;
                    }
                    else if (qUserEvents.StartDate <= DateTime.Now.AddHours(dur17))
                    {
                        // لغو در بازه 17 ساعته
                        // کسر 20 درصد
                        percent = 20;
                    }
                    // درصد مبلغ جریمه محاسبه شده 
                    percentAmount = ((basePrice * percent) / 100);
                    // درصد استاد
                    teacherAmount = ((percentAmount * tPercent) / 100);
                    //درصد سایت
                    siteAmount = percentAmount - teacherAmount;
                    // مبلغ بازگشت به هنرجو
                    payToStudentAmount = basePrice - percentAmount;

                    // اگر قیلا جلسه توسط استاد لغو شده باشد
                    if (qUserEvents.TblUserEventsInvoice == null)
                    {
                        TblUserEventsInvoice t = new TblUserEventsInvoice();
                        t.Date = DateTime.Now;
                        // سود سایت
                        t.ProfitOfAddition = percentAmount - teacherAmount;

                        // سود استاد
                        t.AmountOfAddition = teacherAmount;
                        teacherfinalAmount = teacherAmount;

                        t.Description = " جلسه در تاریخ" + t.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm") + " توسط هنرجو لغو گردید و " + percent +
                            " درصد بابت جریمه به حساب شما منتقل شد";
                        t.TeacherID = qUserEvents.TblOnlineCourse.TeacherID;
                        t.UserEventID = qUserEvents.ID;
                        _context.Add(t);
                    }
                    else
                    {
                        // مبلغ خالص استاد به همراه کسر جریمه
                        int NetAmount = teacherAmount - qUserEvents.TblUserEventsInvoice.AmountOfFines;
                        // مبلغ خالص سایت به اضافه جریمه
                        int SiteNetAmount = siteAmount + qUserEvents.TblUserEventsInvoice.AmountOfFines;

                        // سود سایت
                        qUserEvents.TblUserEventsInvoice.ProfitOfAddition = SiteNetAmount;

                        // سود استاد
                        qUserEvents.TblUserEventsInvoice.AmountOfAddition = NetAmount<0?0: NetAmount;
                        teacherfinalAmount = NetAmount;

                        qUserEvents.TblUserEventsInvoice.Description = " جلسه در تاریخ" + DateTime.Now.ToShamsi().ToString("yyyy/MM/dd - HH:mm") + " توسط هنرجو لغو گردید و " + percent +
                            " درصد بابت جریمه به حساب شما منتقل شد";
                        _context.Update(qUserEvents.TblUserEventsInvoice);
                    }
                    

                    PayRepository Rep_Pay = new PayRepository();
                    string studentFullName = User.GetUserDetails().FirstName + User.GetUserDetails().LastName;
                    if(qUserEvents.TblUserEventsInvoice != null)
                    {
                        if (teacherfinalAmount > 0)
                        {
                            // واریز به حساب استاد
                            await Rep_Pay.DepositToTeacher(_context, _userManager, qUserEvents.TblOnlineCourse.TeacherID,
                                "پرداخت وجه بابت لغو جلسه توسط " + studentFullName + "|" + qUserEvents.TblOnlineCourse.Title, teacherfinalAmount, qUserEvents.TblUserEventsInvoice.ID);
                        }

                        // واریز باقیمانده به هنرجو
                        await Rep_Pay.DepositToTeacher(_context, _userManager, qUserEvents.UserID,
                            "بازگشت وجه بابت لغو جلسه | " + qUserEvents.TblOnlineCourse.Title, payToStudentAmount, qUserEvents.TblUserEventsInvoice.ID);
                    }
                    else
                    {
                        return Json(new { result = "error", msg = "مشکل در واریز وجه لطفا با مدیریت تماس حاصل نمایید" });
                    }


                    qUserEvents.Status = 3;
                    _context.Update(qUserEvents);

                }
                else
                {
                    _context.Remove(qUserEvents);
                }
                qUserClass.CancelCount =+1;
                _context.Update(qUserClass);
                _context.SaveChanges();
                return Json(new { result = "ok", msg = "جلسه با موفقیت لغو گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "error", msg = "خطا در برقراری ارتباط" });
            }
        }

        [HttpPost]
        public IActionResult RemoveEventsTest(int UserClassID,int InvoiceID) 
        {
            var qEvents = _context.TblUserEvents.Where(a => a.InvoiceID == InvoiceID)
                .Include(a=>a.TblUserEventsInvoice).ToList();

            foreach (var item in qEvents.Where(a=>a.TblUserEventsInvoice!=null))
            {
                _context.Remove(item.TblUserEventsInvoice);
            }
            _context.RemoveRange(qEvents);
            _context.SaveChanges();

            return RedirectToAction(nameof(MyClassScheduling),new { ID= UserClassID });
        }
        [HttpPost]
        public async Task<JsonResult> CancelClass(int ID)
        {
            // لغو کلاس و تسویه حساب
            try
            {
                var qUserClass = _context.TblUserOnlineCourse.Where(a => a.ID == ID)
                    .Include(a => a.TblBookingInvoice)
                    .Include(a => a.TblOnlineCourse).SingleOrDefault();
                if (qUserClass.Status == 2)
                {
                    return Json(new { result = "error", msg = "امکان لغو کلاس های اتمام یافته را ندارید" });
                }
                var qUserEvents = _context.TblUserEvents.Where(a => a.InvoiceID == qUserClass.InvoiceID)
                    .Include(a=>a.TblUserEventsInvoice).ToList();

                int PayBackAmount = 0;
                int finalPrice = 0;
                int totalPrice = qUserClass.TblBookingInvoice.Amount;
                int basePrice = (totalPrice) / qUserClass.Package;
                //int amountOfFines = 0;

                // محاسبه تعداد جلسات رزرو نشده
                int RemainingPackage = qUserClass.Package- qUserEvents.Count();
                // محاسبه مبلغ جلسات رزرو نشده
                int RemainingPrice = RemainingPackage * basePrice;
                if (qUserEvents.Count() > 0)
                {
                    foreach (var item in qUserEvents)
                    {
                        if (item.Status == 0 && item.StartDate < DateTime.Now.AddHours(17))
                        {
                            return Json(new { result = "error", msg = "لطفا ابتدا جلسه هایی که در بازه 17 ساعت تا برگزاری قرار دارند را لغو نمایید سپس اقدام به لغو کلاس کنید" });
                        }
                        if (item.TblUserEventsInvoice != null)
                        {
                            //else
                            //{
                            //    // مبلغ جریمه جلسه های استاد
                            //    amountOfFines += item.TblUserEventsInvoice.AmountOfFines;
                            //}
                            if (item.Status == 0 || item.Status==2)
                            {
                                item.Status = 4;
                                PayBackAmount += basePrice;
                                _context.Update(item);
                            }
                        }
                    }

                }
                // تعداد جلسه های رزرو شده
                //int ReservedCount = qUserEvents.Where(a => a.Status == 2).Count();
                // تعداد جلسه های باقیمانده
                //int RemainingCount = qUserClass.Package - ReservedCount;

                //int tPercent = qUserClass.TblBookingInvoice.TeacherPercent > 0 ? qUserClass.TblBookingInvoice.TeacherPercent : qUserClass.TblOnlineCourse.TeacherPercent;

                // حق الزحمه سایت = مبلغ کل - دستمزد استاد
                //int WagePrice = totalPrice - ((totalPrice * tPercent) / 100);

                // کسر 10 درصد بابت لغو کلاس
                //int percentAmount = (totalPrice * 10) / 100;
                //----
                //finalPrice = totalPrice - (PayBackAmount + percentAmount);
                finalPrice = PayBackAmount + RemainingPrice;
                //qUserClass.CancelAmount = percentAmount;

                PayRepository Rep_Pay = new PayRepository();
                // واریز باقیمانده به حساب کاربر
                #region ForUser
                await Rep_Pay.DepositToStudent(_context, _userManager, qUserClass.UserID,
                    "بازگشت وجه به دلیل لغو کلاس | " + qUserClass.TblOnlineCourse.Title, finalPrice, 0);

                #endregion

                // در صورتی مبلغ جریمه ای وجود نداشته باشد به استاد می رسد در غیر این صورت به سایت می رسد
                //if (amountOfFines <= 0)
                //{
                //    // واریز به استاد
                //    #region ForTeacher
                //    var qTeacher = await _userManager.FindByIdAsync(qUserClass.TblOnlineCourse.TeacherID);
                //    // هشتاد درصد مبلغ برای استاد و / باقیمانه برای سایت
                //    int teacherPercentAmount = (percentAmount * 80) / 100;

                //    string studentFullName = User.GetUserDetails().FirstName + User.GetUserDetails().LastName;

                //    await Rep_Pay.DepositToTeacher(_context, _userManager, qUserClass.TblOnlineCourse.TeacherID,
                //    "واریز وجه اضافه  به دلیل لغو کلاس توسط " + studentFullName + "|"+ qUserClass.TblOnlineCourse.Title, teacherPercentAmount);
                //    #endregion
                //}

                qUserClass.Status = 3;
                _context.Update(qUserClass);
                _context.SaveChanges();
                return Json(new { result = "ok", msg = "کلاس با موفقیت لغو گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "error", msg = "خطا در برقراری ارتباط" });
            }
        }
        [HttpPost]
        public async Task<JsonResult> CommentSend(TblComment t, decimal Rating= 5)
        {
            // ارسال دیدگاه در دوره آموزشی توسط هنرجو
            try
            {
                string ReciverID = "";
                string Email = "";
                string smsNumber = "";
                var qCourse = _context.TblCourse.Where(a => a.ID == t.CourseID).Include(a=>a.TblUser).SingleOrDefault();

                if (t.ReplyID > 0)
                {
                    var qComment = _context.TblComment.Where(a => a.ID == t.ReplyID).Include(a=>a.TblUserSender).SingleOrDefault();
                    ReciverID = qComment.UserSender;
                    Email = qComment.TblUserSender.Email!=null? qComment.TblUserSender.Email:"";
                    smsNumber = qComment.TblUserSender.Mobile!=null? qComment.TblUserSender.Mobile:"";
                }
                else
                {
                    ReciverID = qCourse.TeacherID;
                    Email = qCourse.TblUser.Email!=null? qCourse.TblUser.Email:"";
                    smsNumber = qCourse.TblUser.Mobile!=null? qCourse.TblUser.Mobile:"";
                }
                var UserID = User.GetUserID();
                t.UserSender = UserID;
                t.UserReciver = ReciverID;
                t.Date = DateTime.Now;
                t.Status = 0;
                if (User.IsInRole("Admin")|| User.IsInRole("Teacher"))
                {
                    t.Status = 1;
                }
                _context.Add(t);

                var qReview = _context.TblCourseReview.Where(a => a.CourseID == t.CourseID);

                int RCount = qReview.Count();
                var UserRating = qReview.Where(a => a.UserID == UserID).Where(a => a.CourseID == t.CourseID).FirstOrDefault();
                if (UserRating != null)
                {
                    UserRating.Rating = Rating;
                    _context.Update(UserRating);
                }
                else
                {
                    TblCourseReview tr = new TblCourseReview();
                    tr.UserID = UserID;
                    tr.CourseID = (int)t.CourseID;
                    tr.Rating = Rating;
                    _context.Add(tr);

                    RCount += 1;
                }
                _context.SaveChanges();

                #region SendNotif
                string link = "https://" + Request.Host + "/CoursePreview/" + qCourse.ShortLink.Replace(" ", "-")+"/"+ qCourse.ID;
                if (!string.IsNullOrEmpty(Email))
                {
                    string EmailTemplate = "";
                    using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                    {
                        EmailTemplate = reader.ReadToEnd();
                    }
                    //string text = "جهت مشاهده دیدگاه خود بر روی لینک زیر کلیک نمایید";
                    string text = "";
                    string title = "";
                    if (t.Status == 1 && t.ReplyID > 0)
                    {
                        // اعلان دیدگاه پاسخ داده شده به کاربر
                        title = "پاسخ به دیدگاه شما – آکادمی آرمیس";
                        text = "کاربر گرامی به دیدگاه شما در صفحه " + qCourse.Title + " پاسخ داده شد. ";

                        string Body = EmailTemplate.Replace("[TITLE]", title).Replace("[TEXT]", text)
                        .Replace("[LINK]", link).Replace("[LINK-TITLE]", "مشاهده");
                        await _emailSender.SendEmailNoticesAsync(Email, "دریافت دیدگاه جدید", Body);
                    }
                    else if (t.Status != 1)
                    {
                        // اعلان دیدگاه به نویسنده پست
                        title = "ارسال نظر برای دوره شما";
                        text = qCourse.TblUser.FirstName + " " + qCourse.TblUser.LastName + " گرامی برای پست شما در دوره آکادمی آرمیس نظری ارسال شده است";

                        string Body = EmailTemplate.Replace("[TITLE]", title).Replace("[TEXT]", text)
                        .Replace("[LINK]", link).Replace("[LINK-TITLE]", "مشاهده");
                        await _emailSender.SendEmailNoticesAsync(Email, "دریافت دیدگاه جدید", Body);
                    }
                }
                else if (smsNumber != "")
                {
                    SmsSender sms = new SmsSender();
                    string smsText = "";

                    if (t.Status == 1 && t.ReplyID > 0)
                    {
                        smsText = " هنرجوی عزیز به دیدگاه شما پاسخ داده شد.برای مشاهده روی لینک کلیک کنید" + " " + Environment.NewLine +" "+ link;
                    }
                    else if (t.Status != 1)
                    {
                        smsText = qCourse.TblUser.FirstName + " " + qCourse.TblUser.LastName + "گرامی برای دوره شما نظری ارسال شده است برای مشاهده روی لینک کلیک نمایید"
                            + " " + Environment.NewLine + " " + link;
                    }
                    string mCodeSender = sms.SendSms(smsText, smsNumber);
                }
                #endregion

                var qUserCourse = _context.TblUserCourse.Where(a => a.CourseID == t.CourseID).Where(a => a.UserID == UserID)
                .FirstOrDefault();
                string ISBuy = "";
                if (qUserCourse != null)
                {
                    ISBuy = "<div class='c-comments__buyer-badge mr-2'>خریداری کرده</div>";
                }
                string Message = "";
                // امتیاز دهی
                var qUserScore = _context.TblUserScore.Where(a => a.UserID == UserID).Where(a => a.CourseID == t.CourseID).FirstOrDefault();
                if (qUserScore == null)
                {
                    var qScore = _context.TblScore.Where(a => a.TitleEn == "CourseReview").SingleOrDefault();
                    Message = "با تشکر . دیدگاه شما بعد از تایید نمایش داده می شود و  " + qScore.Value + " امتیاز به شما تعلق می گیرد";
                }
                else
                {
                    Message = "با تشکر . دیدگاه شما بعد از تایید نمایش داده می شود";
                }

                FileRepository RepImg = new FileRepository();
                var user = _userManager.FindByIdAsync(UserID).Result;
                var Image = RepImg.GetImageByID(user.ImageID);

                string fullName = user.FirstName + " " + user.LastName;
                string ProfileImg = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                return Json(new { result = "ok", name = fullName, image = ProfileImg, text = t.Text, isBuy= ISBuy, msg= Message ,rating=Rating });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال !" });
            }

        }

        [HttpPost]
        public JsonResult Placement(TblPlacement t, int CourseID, string TheoreticalLevel="",string SheetLevel="")
        {
            try
            {
                string TeacherID = _context.TblCourse.Where(a => a.ID == CourseID).SingleOrDefault().TeacherID;
                t.UserID = User.GetUserID();
                //var qRequest = _context.TblPlacement.Where(a => a.UserID == t.UserID).Where(a => a.CourseID == t.CourseID).SingleOrDefault();
                //if (qRequest != null)
                //{
                //    return Json(new { result = "faill", msg = "برای این دوره قبلا تعیین سطح انجام شده است" });
                //}
                TblTicket tk = new TblTicket();
                tk.CourseID = CourseID;
                tk.RecivedID = TeacherID;
                tk.Title = "تعیین سطح";
                tk.UserID = t.UserID;
                tk.Priority = 2;
                tk.Section = 5;
                tk.Date = DateTime.Now;
                _context.Add(tk);
                _context.SaveChanges();


                TblTicketMsg msg = new TblTicketMsg();
                msg.Date = DateTime.Now;
                msg.Text = "درخواست تعیین سطح - سطح خواندن نت ها : "+ TheoreticalLevel + " | سطح دانستنی های تئوری : "+SheetLevel +
                    " | مدت آموزش (ماه) : "+t.TrainingPeriod + " | سایر توضیحات : "+t.StudentDescription + "";
                msg.ReceiverID = TeacherID;
                msg.SenderID = t.UserID;
                msg.TicketID = tk.ID;
                _context.Add(msg);
                _context.SaveChanges();

                return Json(new { result = "ok", msg = "درخواست شما با موفقیت ارسال گردید. بزودی و پس از بررسی از طرف آکادمی موسیقی آرمیس با شما تماس گرفته خواهد شد" });
            }
            catch (Exception e)
            {
                var text = e.InnerException;
                return Json(new { result = "faill", msg = "خطا در بروزرسانی. لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        public IActionResult Placement(int Page = 1, int Status = -1, string Name = "", string Family = "")
        {
            string UserID = User.GetUserID();
            var qPalcement = _context.TblPlacement.Where(a=>a.UserID== UserID)
                .Include(a => a.TblUser)
                .OrderByDescending(a => a.ID).AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                qPalcement = qPalcement.Where(a => a.TblUser.FirstName.Contains(Name));
            }
            if (!string.IsNullOrEmpty(Family))
            {
                qPalcement = qPalcement.Where(a => a.TblUser.LastName.Contains(Family));
            }
            if (Status >= 0)
            {
                qPalcement = qPalcement.Where(a => a.Status == Status);
            }

            int Take = 14;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = qPalcement.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            ViewData["PageTitle"] = "تعیین سطح";
            ViewData["currentTab"] = "support";
            ViewData["current_subTab"] = "placement";
            return View(qPalcement.Skip(Skip).Take(Take).ToList());
        }
        [HttpPost]
        public IActionResult SendMessage(VmChat vm)
        {
            try
            {
                var qChat = _context.TblChat.Where(a => a.ReceiverID == vm.ReceiverID).Where(a => a.UserID == vm.SenderID).SingleOrDefault();
                string UserID = User.GetUserID();
                if (qChat != null)
                {
                    TblMessages t = new TblMessages();
                    t.Text = vm.Text;
                    t.SenderID = UserID;
                    t.ChatID = qChat.ID;
                    t.ReaseverID = vm.ReceiverID;
                    t.Date = t.Date;
                    _context.Add(t);
                    _context.SaveChanges();
                }
                else
                {
                    TblChat chat = new TblChat();
                    chat.UserID = UserID;
                    chat.ReceiverID = vm.ReceiverID;
                    chat.Date = DateTime.Now;
                    chat.Notif = true;
                    _context.Add(chat);
                    _context.SaveChanges();

                    TblMessages t = new TblMessages();
                    t.Text = vm.Text;
                    t.SenderID = UserID;
                    t.ChatID = chat.ID;
                    t.ReaseverID = vm.ReceiverID;
                    t.Date = DateTime.Now;
                    _context.Add(t);
                    _context.SaveChanges();
                }
                TempData["Style"] = "alert-success";
                TempData["Message"] = "پیام شما با موفقیت ارسال گردید";
            }
            catch (Exception)
            {
                TempData["Message"] = "خطا در عملیات ! لطفا در یک زمان دیگر سعی نمایید";
                TempData["Style"] = "alert-danger";
            }
            string UserName = _userManager.FindByIdAsync(vm.ReceiverID).Result.UserName;
            return View();
        }
        [Route("Profile/Support")]
        public IActionResult Support()
        {
            var UserID = User.GetUserID();
            var qSupport = _context.TblTicket.Where(a => a.UserID == UserID).OrderByDescending(a => a.ID).AsQueryable();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "تیکت ها";
            ViewData["currentNav"] = "support";
            return View();
        }
        public JsonResult GetSupport()
        {
            string UserID = User.GetUserID();
            var qSupport = _context.TblTicket.Where(a => a.UserID == UserID).OrderByDescending(a => a.ID).AsQueryable();

            List<VmTicket> Lstvm = new List<VmTicket>();
            foreach (var item in qSupport)
            {
                VmTicket vm = new VmTicket();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Status = item.Status;
                vm.Priority = item.Priority;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                Lstvm.Add(vm);
            }
            return Json(Lstvm);
        }
        [Route("Profile/SupportRequest")]
        public IActionResult SupportRequest()
        {
            var UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "درخواست پشتیبانی";
            ViewData["currentNav"] = "support";
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> SupportRequest(TblTicket t, string Text, int SendInfo, IFormFile File)
        {
            try
            {
                t.Date = DateTime.Now;
                t.UserID = User.GetUserID();
                t.Status = 1;
                t.RecivedID = _userManager.GetUsersInRoleAsync("Admin").Result.FirstOrDefault().Id;
                if (SendInfo == 1)
                {
                    t.BeNotified = true;
                }
                _context.Add(t);
                _context.SaveChanges();

                TblTicketMsg msg = new TblTicketMsg();
                msg.Date = DateTime.Now;
                msg.Text = Text;
                msg.ReceiverID = _userManager.GetUsersInRoleAsync("Admin").Result.FirstOrDefault().Id;
                msg.SenderID = User.GetUserID();
                msg.TicketID = t.ID;

                if (File != null)
                {
                    if (File.Length > 235929600)
                    {
                        return Json(new { result = "faill", msg = "حجم فایل انتخاب شده بیش از حد مجاز است" });
                    }
                    Random rnd = new Random();
                    string format = Path.GetExtension(File.FileName);
                    string[] name = File.FileName.Split(new string[] { "," }, StringSplitOptions.None);

                    Ftp MyFtp = new Ftp();
                    string FileName = "Attached" + DateTime.Now.ToString("yyyyMMddhhmmss") + format;
                    int FtpID = MyFtp.Upload("files", FileName, File.OpenReadStream());
                    if (FtpID != -1)
                    {
                        TblFiles file = new TblFiles();
                        file.ServerID = FtpID;
                        file.UserID = User.GetUserID();
                        file.Title = name[0];
                        file.FileName = FileName;
                        file.Alt = "فایل ضمیمه";
                        _context.Add(file);
                        _context.SaveChanges();
                        msg.FileID = file.ID;
                    }
                }
                _context.Add(msg);
                _context.SaveChanges();

                #region Notices To Admin
                string AdminEmail = "krm.insert@gmail.com";

                string EmailTemplate = "";
                string Link = "https://armisacademy.com/Admin/SupportDetails/" + t.ID;
                using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email.html")))
                {
                    EmailTemplate = reader.ReadToEnd();
                }
                //string text = "جهت مشاهده جزئیات تیکت بر روی مشاهده کلیک نمایید";
                string Body = EmailTemplate.Replace("[TITLE]", "تیکت جدید در آرمیس آکادمی").Replace("[TEXT]", Text)
                    .Replace("[SENDER]", User.GetUserDetails().FirstName + " " + User.GetUserDetails().LastName)
                    .Replace("[COURSE]", "-")
                    .Replace("[LINK]", HtmlEncoder.Default.Encode(Link)).Replace("[LINK-TITLE]", "مشاهده");
                await _emailSender.SendEmailNoticAsync(AdminEmail, "تیکت جدید", Body);
                #endregion

                TempData["Message"] = "تیکت جدید با موفقیت ارسال گردید";
                TempData["Style"] = "alert-success";
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                TempData["Message"] = "خطای غیر منتظره! لطفا در زمان دیگری تلاش نمایید";
                TempData["Style"] = "alert-danger";
            }
            return RedirectToAction(nameof(Support));
        }
        [HttpPost]
        public IActionResult Support(TblTicket t, string Text)
        {
            try
            {
                var UserID = User.GetUserID();

                t.Date = DateTime.Now;
                t.Status = 0;
                t.UserID = UserID;
                _context.Add(t);
                _context.SaveChanges();

                TblTicketMsg msg = new TblTicketMsg();
                msg.TicketID = t.ID;
                msg.Text = Text;
                msg.SenderID = UserID;
                msg.Date = DateTime.Now;
                // ارسال به مدیر . و جهت پاسخگویی برای نقش های همکار نیز نمایش داده خواهد شد
                msg.ReceiverID = _userManager.GetUsersInRoleAsync("Admin").Result.FirstOrDefault().Id;
                //
                //t.TblTicketMsg.Add(msg);
                _context.Add(msg);
                _context.SaveChanges();

                TempData["Message"] = "تیکت شما با موفقیت ثبت گردید و پس از بررسی نتیجه آن به شما اعلام خواهد شد";
                TempData["Style"] = "alert-success";
            }
            catch (Exception)
            {
                TempData["Message"] = "خطای غیر منتظره ای رخ داد لطفا در زمان دیگری تلاش نمایید";
                TempData["Style"] = "alert-danger";
            }
            return RedirectToAction(nameof(Support));
        }
        [HttpGet]
        public async Task <IActionResult> Conversations(int Page = 1,int Read = 0)
        {
            string UserID = User.GetUserID();
            var qSupport = _context.TblChat.Where(a=>a.UserID== UserID)
                .Include(a => a.TblMessages).ThenInclude(a => a.TblCourse)
                .Include(a => a.TblUser)
                .OrderByDescending(a => a.Date).AsEnumerable();

            //if (CourseID > 0)
            //{
            //    qSupport = qSupport.Where(a => a.TblMessages.CourseID == CourseID);
            //}
            if (Read == 1)
            {
                qSupport = qSupport.Where(a => a.Read == false);
            }
            else if (Read == 2)
            {
                qSupport = qSupport.Where(a => a.Read == true);
            }

            List<VmMessage> Lstvm = new List<VmMessage>();
            foreach (var itemMain in qSupport)
            {
                var qTeacher = await _userManager.FindByIdAsync(itemMain.ReceiverID);
                VmMessage vm = new VmMessage();
                vm.ID = itemMain.ID;
                vm.SenderName = qTeacher.FirstName + " " + qTeacher.LastName;
                vm.CourseName = itemMain.TblMessages.FirstOrDefault().TblCourse.Title;
                vm.Date = itemMain.Date.ToShamsi().ToString("yyyy/MM/dd - hh:mm");
                vm.Read = true;
                if (itemMain.TblMessages.Where(a => a.Read == false).Where(a => a.ReaseverID == UserID).ToList().Count() > 0)
                {
                    vm.Read = false;
                }
                vm.Level = (int)itemMain.TblMessages.FirstOrDefault().Level;
                Lstvm.Add(vm);
            }
            int Take = 14;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = Lstvm.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            ViewData["PageTitle"] = "ارتباط با استاد";
            ViewData["currentTab"] = "support";
            ViewData["current_subTab"] = "conv";
            return View(Lstvm.Skip(Skip).Take(Take).ToList());
        }
        [HttpGet]
        public IActionResult ConvDetails(int ID)
        {
            string userID = User.GetUserID();
            var qSupport = _context.TblMessages.Where(a => a.ChatID == ID)
                .Include(a => a.TblChat)
                .Include(a => a.TblUser)
                .Include(a => a.TblFiles)
                .OrderByDescending(a => a.Date)
                .ToList();

            List<VmMessage> lstMessage = new List<VmMessage>();
            FileRepository RepImg = new FileRepository();
            var UserID = User.GetUserID();
            foreach (var item in qSupport)
            {
                if (item.TblChat.UserID != UserID)
                {
                    TempData["Message"] = "شما دسترسی لازم برای مشاهده صفحه مورد نظر را ندارید";
                    TempData["Style"] = "alert-danger";
                    return RedirectToAction(nameof(Conversations));
                }
                if (item.ReaseverID == UserID)
                {
                    if (item.Read == false)
                    {
                        item.Read = true;
                        _context.Update(item);
                    }
                }
                var chatImage = RepImg.GetImageByID(item.TblUser.ImageID);
                VmMessage msg = new VmMessage();
                msg.Text = item.Text;
                msg.SenderID = item.SenderID;
                msg.Date = item.Date.ToShamsi().ToString();
                msg.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                msg.ProfileImage = chatImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + chatImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + chatImage.FileName;

                if (item.TblFiles != null)
                {
                    msg.File = new MsgFile();
                    msg.File.ID = item.TblFiles.ID;
                    var file = RepImg.GetFileByID(item.TblFiles.ID);
                    msg.File.Link = file.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + file.TblServer.Path.Trim(new char[] { '/' }) + "/" + file.FileName; ;
                    msg.File.Title = item.TblFiles.Title;
                }
                lstMessage.Add(msg);
            }
            _context.SaveChanges();
            ViewBag.CourseID = qSupport.FirstOrDefault().CourseID;
            ViewBag.ReaseverID = qSupport.FirstOrDefault().TblChat.ReceiverID;
            ViewBag.ChatID = qSupport.FirstOrDefault().ChatID;
            ViewBag.Level = qSupport.FirstOrDefault().Level;

            ViewData["PageTitle"] = "پشتیبانی";
            ViewData["currentTab"] = "support";
            ViewData["current_subTab"] = "conv";
            return View(lstMessage);
        }
        public JsonResult SendConversionMsg(TblMessages t)
        {
            // ارتباط با هنرجو
            try
            {
                FileRepository RepImg = new FileRepository();
                var qUser = User.GetUserDetails();

                t.Date = DateTime.Now;
                t.Read = false;
                t.SenderID = User.GetUserID();

                if (t.File != null)
                {
                    if (t.File.Length > 25414385)
                    {
                        return Json(new { result = "faill", msg = "حجم فایل انتخاب شده بیش از حد مجاز است" });
                    }
                    Random rnd = new Random();
                    string format = Path.GetExtension(t.File.FileName);
                    string[] name = t.File.FileName.Split(new string[] { "," }, StringSplitOptions.None);

                    Ftp MyFtp = new Ftp();
                    string FileName = "Attached" + DateTime.Now.ToString("yyyyMMddhhmmss") + format;
                    int FtpID = MyFtp.Upload("files", FileName, t.File.OpenReadStream());
                    if (FtpID != -1)
                    {
                        TblFiles file = new TblFiles();
                        file.ServerID = FtpID;
                        file.UserID = User.GetUserID();
                        file.Title = name[0];
                        file.FileName = FileName;
                        file.Alt = "فایل ضمیمه ";
                        _context.Add(file);
                        _context.SaveChanges();
                        t.FileID = file.ID;
                    }
                }
                _context.Add(t);
                var qChat = _context.TblChat.Where(a => a.ID == t.ChatID).SingleOrDefault();
                if (qChat.Read == true)
                {
                    qChat.Read = false;
                    _context.Update(qChat);
                }
                _context.SaveChanges();

                string FullName = qUser.FirstName + " " + qUser.LastName;
                var chatImage = RepImg.GetImageByID(qUser.ImageID);
                string ProfileImage = chatImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + chatImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + chatImage.FileName;

                return Json(new { result = "ok", text = t.Text, senderName = FullName, image = ProfileImage });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال . لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        [HttpPost]
        public async Task<JsonResult> CourseConversion(TblMessages t, int CourseID)
        {
            // قسمت ارتباط با استاد در قسمت دوره آموزشی
            try
            {
                var UserID = User.GetUserID();
                var qTeacher = _context.TblCourse.Where(a => a.ID == CourseID)
                    .Include(a => a.TblUser)
                    .SingleOrDefault();

                var qMessages = _context.TblMessages.Where(a => a.SenderID == UserID).Where(a => a.CourseID == t.CourseID).FirstOrDefault();

                t.Date = DateTime.Now;
                t.Read = false;
                t.SenderID = UserID;
                t.ReaseverID = qTeacher.TblUser.Id;
                if (qMessages == null)
                {
                    t.TblChat = new TblChat();
                    t.TblChat.ReceiverID = qTeacher.TblUser.Id;
                    t.TblChat.UserID = UserID;
                    t.TblChat.Date = DateTime.Now;
                    t.TblChat.Type = 1;
                }
                else
                {
                    var qChat = _context.TblChat.Where(a => a.ID == qMessages.ChatID).SingleOrDefault();

                    qChat.Read = false;
                    qChat.Date = DateTime.Now;
                    _context.Update(qChat);
                }
                if (t.File != null)
                {
                    if (t.File.Length > 25414385)
                    {
                        return Json(new { result = "faill", msg = "حجم فایل انتخاب شده بیش از حد مجاز است" });
                    }
                    Random rnd = new Random();
                    string format = Path.GetExtension(t.File.FileName);
                    string[] name = t.File.FileName.Split(new string[] { "," }, StringSplitOptions.None);

                    Ftp MyFtp = new Ftp();
                    string FileName = "Attached" + DateTime.Now.ToString("yyyyMMddhhmmss") + format;
                    int FtpID = MyFtp.Upload("files", FileName, t.File.OpenReadStream());
                    if (FtpID != -1)
                    {
                        Guid originalGuid = Guid.NewGuid();
                        string stringGuids = originalGuid.ToString("N");

                        TblFiles file = new TblFiles();
                        file.ServerID = FtpID;
                        file.UserID = User.GetUserID();
                        file.Title = name[0];
                        file.FileName = FileName;
                        file.Alt = "فایل ضمیمه";
                        file.Token = stringGuids;
                        _context.Add(file);
                        _context.SaveChanges();
                        t.FileID = file.ID;
                    }
                }

                _context.Add(t);
                _context.SaveChanges();

                string CourseName = _context.TblCourse.Where(a => a.ID == CourseID).SingleOrDefault().Title;
                if (User.IsInRole("Student"))
                {
                    #region Notices To Admin & Teacher
                    string AdminEmail = "krm.insert@gmail.com";

                    string EmailTemplate = "";
                    string Link = "https://armisacademy.com/Admin/ConvDetails/" + t.ID;

                    using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email.html")))
                    {
                        EmailTemplate = reader.ReadToEnd();
                    }
                    //string text = "جهت مشاهده جزئیات تیکت بر روی مشاهده کلیک نمایید";
                    string Body = EmailTemplate.Replace("[TITLE]", "تیکت جدید").Replace("[TEXT]", t.Text)
                        .Replace("[SENDER]", User.GetUserDetails().FirstName + " " + User.GetUserDetails().LastName)
                        .Replace("[COURSE]", CourseName)
                        .Replace("[LINK]", HtmlEncoder.Default.Encode(Link)).Replace("[LINK-TITLE]", "مشاهده");

                    await _emailSender.SendEmailNoticAsync(AdminEmail, "ارتباط با استاد در آرمیس آکادمی", Body);

                    // send for teacher
                    if (!string.IsNullOrEmpty(qTeacher.TblUser.Email))
                    {
                        string Link2 = "https://armisacademy.com/Teacher/ConvDetails/" + t.ID;

                        await _emailSender.SendEmailNoticAsync(qTeacher.TblUser.Email, "ارتباط با استاد جدید در آرمیس آکادمی", Body);
                    }
                    if (!string.IsNullOrEmpty(qTeacher.TblUser.Mobile))
                    {
                        
                        SmsSender sms = new SmsSender();
                        string smsText = "هنرجوی شما در دوره " + qTeacher.Title +" پیامی ارسال کرده است." + Environment.NewLine +" "+ "https://Armisacademy.com";
                        string mCodeSender = sms.SendSms(smsText, qTeacher.TblUser.Mobile);
                    }
                    #endregion
                }

                FileRepository RepImg = new FileRepository();
                var user = _userManager.FindByIdAsync(UserID).Result;
                var Image = RepImg.GetImageByID(user.ImageID);

                string fullName = user.FirstName + " " + user.LastName;
                string ProfileImg = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                return Json(new { result = "ok", name = fullName, image = ProfileImg, text = t.Text, msg = "پیام شما با موفقیت ارسال گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطای غیر منتظره ای رخ داد لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        [HttpGet]
        [Route("Profile/SupportDetails/{ID}")]
        public IActionResult SupportDetails(int ID)
        {
            var UserID = User.GetUserID();

            var qSupport = _context.TblTicket.Where(a => a.ID == ID)
                .Include(a => a.TblUser)
                .Include(a => a.TblTicketMsg).ThenInclude(a => a.TblUserSender)
                .Include(a => a.TblTicketMsg).ThenInclude(a => a.TblFiles)
                .SingleOrDefault();

            if (qSupport.UserID != User.GetUserID())
            {
                TempData["Message"] = "شما دسترسی لازم برای مشاهده صفحه مورد نظر را ندارید";
                TempData["Style"] = "alert-danger";
                return RedirectToAction(nameof(Support));
            }
            FileRepository RepImg = new FileRepository();
            VmTicket vm = new VmTicket();

            vm.ID = qSupport.ID;
            vm.Email = string.IsNullOrEmpty(qSupport.TblUser.Email) ? "ثبت نشده" : qSupport.TblUser.Email;
            vm.Date = qSupport.Date.ToShamsi().ToString("yyyy/MM/dd HH:mm");
            vm.UserName = qSupport.TblUser.UserName;
            vm.UserID = qSupport.TblUser.Id;
            vm.Title = qSupport.Title;
            vm.BeNotified = qSupport.BeNotified;
            vm.Status = qSupport.Status;
            vm.Priority = qSupport.Priority;

            if (qSupport.CourseID > 0)
            {
                vm.CourseName = qSupport.TblCourse.Title;
            }

            if (qSupport.Status == 0)
            {
                qSupport.Status = 1;
                _context.Update(qSupport);
            }

            vm.ListTicketMsg = new List<TicketMsg>();
            foreach (var item in qSupport.TblTicketMsg.OrderByDescending(a => a.Date))
            {
                if (item.Read == false)
                {
                    item.Read = true;
                    _context.Update(item);
                }
                TicketMsg msg = new TicketMsg();
                msg.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - hh:mm");
                msg.SenderID = item.SenderID;
                msg.Text = item.Text;
                msg.SenderName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                var chatImage = RepImg.GetImageByID(item.TblUserSender.ImageID);
                msg.ProfileImage = chatImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + chatImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + chatImage.FileName;

                if (item.TblFiles != null)
                {
                    msg.File = new MessageFile();
                    msg.File.ID = item.TblFiles.ID;
                    var file = RepImg.GetFileByID(item.TblFiles.ID);
                    msg.File.Link = file.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + file.TblServer.Path.Trim(new char[] { '/' }) + "/" + file.FileName; ;
                    msg.File.Title = item.TblFiles.Title;
                }
                vm.ListTicketMsg.Add(msg);
            }
            _context.SaveChanges();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "جزئیات پشتیبانی";
            ViewData["currentNav"] = "support";
            return View(vm);
        }
        [HttpPost]
        public JsonResult SendTicketMsg(TblTicketMsg t, int SendInfo)
        {
            try
            {
                var qSupport = _context.TblTicket.Where(a => a.ID == t.TicketID).SingleOrDefault();
                if (qSupport.UserID != User.GetUserID())
                {
                    return Json("");
                }

                t.Date = DateTime.Now;
                t.SenderID = User.GetUserID();
                t.ReceiverID = qSupport.RecivedID;

                if (t.File != null)
                {
                    if (t.File.Length > 235929600)
                    {
                        return Json(new { result = "faill", msg = "حجم فایل انتخاب شده بیش از حد مجاز است" });
                    }
                    Random rnd = new Random();
                    string format = Path.GetExtension(t.File.FileName);
                    string[] name = t.File.FileName.Split(new string[] { "," }, StringSplitOptions.None);

                    Ftp MyFtp = new Ftp();
                    string FileName = "Attached" + DateTime.Now.ToString("yyyyMMddhhmmss") + format;
                    int FtpID = MyFtp.Upload("files", FileName, t.File.OpenReadStream());
                    if (FtpID != -1)
                    {
                        Guid originalGuid = Guid.NewGuid();
                        string stringGuids = originalGuid.ToString("N");

                        TblFiles file = new TblFiles();
                        file.ServerID = FtpID;
                        file.UserID = User.GetUserID();
                        file.Title = name[0];
                        file.FileName = FileName;
                        file.Alt = "فایل ضمیمه ";
                        file.Token = stringGuids;
                        _context.Add(file);
                        _context.SaveChanges();
                        t.FileID = file.ID;
                    }
                }
                if (SendInfo == 1)
                {
                    qSupport.BeNotified = true;
                }
                qSupport.Status = 0;
                _context.Update(qSupport);
                _context.Add(t);
                _context.SaveChanges();

                FileRepository RepImg = new FileRepository();
                var qUser = User.GetUserDetails();

                string FullName = qUser.FirstName + " " + qUser.LastName;
                var chatImage = RepImg.GetImageByID(qUser.ImageID);
                string ProfileImage = chatImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + chatImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + chatImage.FileName;

                return Json(new { result = "ok", text = t.Text, senderName = FullName, image = ProfileImage });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال . لطفا در زمان دیگری تلاش نمایید" });
            }
        }

        public IActionResult Financial3(int Page = 1, int Status = 0, int Type = 0)
        {
            string UserID = User.GetUserID();
            var qPayment = _context.TblTransaction.Where(a => a.ToUserID == UserID || a.UserID == UserID)
                .Include(a => a.TblInvoice)
                .Include(a => a.TblUser).OrderByDescending(a => a.Date).AsQueryable();
            if (Status > 0)
            {
                qPayment = qPayment.Where(a => a.Status == Status);
            }
            if (Type > 0)
            {
                qPayment = qPayment.Where(a => a.Type == Type);
            }
            int Take = 14;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = qPayment.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);
            ViewData["PageTitle"] = "مالی";
            ViewData["currentTab"] = "more";
            ViewData["current_subTab"] = "Financial";
            ViewData["currentNav"] = "Financial";

            if (qPayment.Count() > 0)
            {
                qPayment = qPayment.Skip(Skip).Take(Take);
            }
            return View(qPayment.ToList());
        }
        [Route("Profile/Financial")]
        public IActionResult Financial()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "گزارش خرید";
            ViewData["currentNav"] = "financial";
            return View();
        }
        public JsonResult GetFinancial()
        {
            string UserID = User.GetUserID();
            var qPayment = _context.TblTransaction.Where(a => a.ToUserID == UserID || a.UserID == UserID)
                .Include(a => a.TblInvoice).Where(a => a.Type == 1 || a.Type == 2 || a.Type == 5)
                .Include(a => a.TblUser).OrderByDescending(a => a.Date).AsQueryable();

            List<VmInvoice> Lstvm = new List<VmInvoice>();
            foreach (var item in qPayment)
            {
                VmInvoice vm = new VmInvoice();
                vm.ID = item.ID;
                vm.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Description = item.Description;
                vm.Type = item.Type;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                vm.Amount = item.Amount;
                vm.BoonID = 0;
                if (item.TblInvoice != null && item.TblInvoice.BoonID != null)
                {
                    vm.BoonID = (int)item.TblInvoice.BoonID;
                }
                Lstvm.Add(vm);
            }
            return Json(Lstvm);
        }
        [Route("Profile/Invoice")]
        public IActionResult Invoice()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "فاکتور ها";
            ViewData["currentNav"] = "invoice";
            return View();
        }
        public JsonResult GetInvoice()
        {
            string UserID = User.GetUserID();
            var qPayment = _context.TblInvoice.Where(a => a.UserID == UserID || a.UserID == UserID)
                .Include(a => a.TblUser).OrderByDescending(a => a.Date).AsQueryable();

            List<VmInvoice> Lstvm = new List<VmInvoice>();
            foreach (var item in qPayment)
            {
                VmInvoice vm = new VmInvoice();
                vm.ID = item.ID;
                vm.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.SaleRefrenceID = item.SaleRefrenceID;
                vm.Status = item.Status;
                vm.Amount = item.Amount;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                Lstvm.Add(vm);
            }
            return Json(Lstvm);
        }

        [HttpGet]
        [Route("Profile/InvoiceDetails/{ID}")]
        public IActionResult InvoiceDetails(int id)
        {
            var qInvoice = _context.TblInvoice.Where(a => a.ID == id)
                .Include(a => a.TblCourse)
                .Include(a => a.TblSession)
                .SingleOrDefault();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(User.GetUserID(), roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "مشاهده فاکتور";
            ViewData["currentNav"] = "invoice";
            return View(qInvoice);
        }
        [HttpPost]
        public JsonResult EditBankDetails(int ID, string BankName, string CardNumber, string ShabaNumber, string AccountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(BankName) || string.IsNullOrEmpty(CardNumber) || string.IsNullOrEmpty(AccountNumber))
                {
                    return Json("notValid");
                }
                if (CardNumber.Trim().Length != 16)
                {
                    return Json(new { result = "faill", msg = "لطفا شماره کارت را به درستی وارد نمااید" });
                }
                var qBank = _context.TblBank.Where(a => a.ID == ID).SingleOrDefault();
                qBank.AccountNumber = AccountNumber;
                qBank.BankName = BankName;
                qBank.CardNumber = CardNumber;
                qBank.ShabaNumber = ShabaNumber;

                _context.Update(qBank);
                _context.SaveChanges();
                return Json("ok");
            }
            catch (Exception)
            {
                return Json(new {result="faill", msg="خطا در ثبت اطلاعات"});
            }
        }
        [HttpPost]
        public JsonResult DelBankDetails(int ID)
        {
            try
            {
                var qBank = _context.TblBank.Where(a => a.ID == ID).SingleOrDefault();

                _context.Remove(qBank);
                _context.SaveChanges();
                return Json("ok");
            }
            catch (Exception)
            {
                return Json("faill");
            }
        }
        [Route("/Profile/AllNotics")]
        public IActionResult AllNotics(int Page = 1)
        {
            string UserID = User.GetUserID();
            var qNotic = _context.TblMessages.Where(a => a.ReaseverID == UserID).Where(a=>a.CourseID!=null)
                .Include(a => a.TblUser)
                .Include(a => a.TblCourse);

            var qMsgTicket = _context.TblTicketMsg.Where(a => a.ReceiverID == UserID)
                .Include(a => a.TblTicket)
                .Include(a => a.TblUserSender);

            List<VmMessage> LstNotic = new List<VmMessage>();
            FileRepository RepImg = new FileRepository();
            TimeUtility time = new TimeUtility();
            foreach (var item in qNotic)
            {
                VmMessage vm = new VmMessage();
                vm.ID = item.ID;
                vm.Read = item.Read;
                vm.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Text = vm.SenderName + " پیامی را ارسال کرده است";
                vm.Link = "/Course/" + item.TblCourse.ShortLink.Replace(" ","-")+"/"+item.CourseID;
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
                vm.Read = item.Read;
                vm.SenderName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                vm.Text = vm.SenderName + " پاسخ جدیدی را در تیکت " + item.TblTicket.Title + " ارسال کرده است ";
                vm.Link = "/Student/SupportDetails/" + item.TicketID;
                vm.Date = time.GetDateName(item.Date);
                vm.DateName = time.GetTimeName(item.Date);
                vm.Time = item.Date.ToString("hh:mm");
                var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                LstNotic.Add(vm);
            }
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            int Take = 18;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = LstNotic.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            var Notics = LstNotic.AsQueryable()
                .OrderByDescending(a => a.Date)
                .OrderByDescending(a => a.Read == false).Skip(Skip).Take(Take).ToList();

            ViewData["currentNav"] = "profile";
            ViewBag.currentTab = "";
            return View(Notics);
        }

        [Route("Terms")]
        public async Task<IActionResult> Terms()
        {
            var user = await _userManager.FindByIdAsync(User.GetUserID());

            string UserID = User.GetUserID();
            VmUser vm = new VmUser();
            FileRepository RepImg = new FileRepository();
            UserRepository RepUser = new UserRepository();
            ViewBag.ProfileProgress = RepUser.getProfileProgres(user.Id);
            vm.ID = user.Id;
            vm.FullName = user.FirstName + " " + user.LastName;
            string roleName = _userManager.GetRolesAsync(user).Result.FirstOrDefault();
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

            var qGuide = _context.TblGuide.Where(a => a.Type == 2).OrderByDescending(a => a.ID).ToList();
            vm.LstTerms = new List<UserrTerms>();
            foreach (var item in qGuide)
            {
                UserrTerms ut = new UserrTerms();
                ut.ID = item.ID;
                ut.Text = item.Text;
                ut.Title = item.Title;
                vm.LstTerms.Add(ut);
            }

            #region UserInfo
            vm.Follow = new UserFollow();
            var qFollow = _context.TblFriends;
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
            ViewData["currentNav"] = "terms";
            return View(vm);
        }
        [AllowAnonymous]
        public IActionResult Error()
        {
            return View();
        }
    }
}
