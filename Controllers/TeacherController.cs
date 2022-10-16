using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.ViewModels;
using ArmisApp.Models.ExMethod;
using ArmisApp.Models.Utility;
using ArmisApp.Models.Repository;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Text.Encodings.Web;
using ArmisApp.Services;
using Microsoft.Extensions.Hosting;
using System.Text;
using System.Text.Json;

namespace ArmisApp.Controllers
{
    [Authorize(Roles = "Teacher,Admin")]
    public class TeacherController : Controller
    {
        private readonly DataContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IHostEnvironment _appEnvironment;
        private IHttpContextAccessor _accessor;
        private readonly IEmailSender _emailSender;

        public TeacherController(
            DataContext context,
            UserManager<ApplicationUser> userManager,
            IEmailSender emailSender,
            IHttpContextAccessor accessor,
            IHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _emailSender = emailSender;
            _appEnvironment = environment;
            _accessor = accessor;
        }

        public IActionResult Index()
        {
            UserRepository RepUser = new UserRepository();

            ViewBag.ProfileProgress = RepUser.getProfileProgres(User.GetUserID());
            ViewData["PageTitle"] = "خانه";
            ViewData["currentTab"] = "home";
            ViewData["current_subTab"] = "home";
            return View();
        }
        public IActionResult Support(int Page = 1, string ID = "", string Title = "", int Status = -1, int Priority = -1)
        {
            var UserID = User.GetUserID();
            var qSupport = _context.TblTicket.Where(a => a.UserID == UserID).OrderByDescending(a => a.ID).AsQueryable();

            if (!string.IsNullOrEmpty(ID))
            {
                qSupport = qSupport.Where(a => a.UserID == ID);
            }
            if (!string.IsNullOrEmpty(Title))
            {
                qSupport = qSupport.Where(a => a.Title == Title);
            }
            if (Status >= 0)
            {
                qSupport = qSupport.Where(a => a.Status == Status);
            }
            if (Priority >= 0)
            {
                qSupport = qSupport.Where(a => a.Priority == Priority);
            }

            int Take = 14;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = qSupport.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            ViewData["PageTitle"] = "پشتیبانی";
            ViewData["currentTab"] = "support";
            ViewData["current_subTab"] = "ticket";
            if (qSupport.Count() > 0)
            {
                qSupport = qSupport.Skip(Skip).Take(Take);
            }
            return View(qSupport.ToList());
        }

        [HttpGet]
        public IActionResult SupportRequest()
        {
            ViewData["currentTab"] = "support";
            ViewData["current_subTab"] = "ticket";
            return View();
        }
        [HttpPost]
        public async Task<IActionResult> SupportRequest(TblTicket t, string Text, int SendInfo, IFormFile File)
        {
            try
            {
                t.Date = DateTime.Now;
                t.UserID = User.GetUserID();
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
                msg.TicketID = t.ID;
                msg.SenderID = User.GetUserID();

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
                    string FileName = "Attached" + DateTime.Now.ToString("yyyyMMddhhmmss") + "." + format;
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
                using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "Email2.html")))
                {
                    EmailTemplate = reader.ReadToEnd();
                }
                //string text = "جهت مشاهده جزئیات تیکت بر روی مشاهده کلیک نمایید";
                string Body = EmailTemplate.Replace("[TITLE]", "تیکت جدید در آرمیس آکادمی").Replace("[TEXT]", Text)
                    .Replace("[LINK]", HtmlEncoder.Default.Encode(Link)).Replace("[LINK-TITLE]", "مشاهده");
                await _emailSender.SendEmailConfirmationAsync(AdminEmail, "تیکت جدید در آرمیس آکادمی", Body);
                #endregion

                TempData["Message"] = "تیکت جدید با موفقیت ارسال گردید";
                TempData["Style"] = "alert-success";
            }
            catch (Exception)
            {
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
        [Route("Profile/Conversations")]
        public IActionResult Conversations()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["currentNav"] = "conv";
            ViewData["breadcrumb"] = "گفتگو ها";
            ViewData["CourseID"] = new SelectList(_context.TblCourse, "Title", "Title");
            return View();
        }
        public JsonResult GetConversations()
        {
            string UserID = User.GetUserID();

            var qSupport = _context.TblChat.Where(a => a.ReceiverID == UserID)
                .Include(a => a.TblMessages).ThenInclude(a => a.TblCourse)
                .Include(a => a.TblUser)
                .OrderByDescending(a => a.LastSentDate).AsEnumerable();

            List<VmMessage> Lstvm = new List<VmMessage>();
            FileRepository RepImg = new FileRepository();
            foreach (var itemMain in qSupport)
            {

                VmMessage vm = new VmMessage();
                vm.ID = itemMain.ID;
                vm.SenderName = itemMain.TblUser.FirstName + " " + itemMain.TblUser.LastName;
                vm.CourseName = itemMain.TblMessages.FirstOrDefault().TblCourse.Title;
                vm.Date = itemMain.Date.ToShamsi().ToString("yyyy/MM/dd");
                vm.LastSentDate = itemMain.LastSentDate.ToShamsi().ToString("yyyy/MM/dd - hh:mm");
                vm.Read = true;
                if (itemMain.TblMessages.Where(a => a.Read == false).Where(a => a.ReaseverID == UserID).ToList().Count() > 0)
                {
                    vm.Read = false;
                }
                var qChat = _context.TblMessages.Where(a => a.ReaseverID == UserID)
                    .Where(a => a.CourseID == itemMain.TblMessages.FirstOrDefault().CourseID)
                    .Where(a => a.SenderID == itemMain.UserID).OrderByDescending(a => a.Date).ToList();
                if (qChat.Count > 0)
                {
                    foreach (var item in qChat)
                    {
                        if (item.ChatID == null)
                        {
                            item.ChatID = itemMain.ID;
                            _context.Update(item);
                        }
                    }
                }
                var Image = RepImg.GetImageByID(itemMain.TblUser.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                Lstvm.Add(vm);
            }
            _context.SaveChanges();
            if (Lstvm.Count() > 0)
            {
                // ابتدا بر اساس تاریخ مرتب می کند و سپس بر اساس خوانده نشده - شکل درست
                Lstvm = Lstvm.OrderByDescending(a => a.LastSentDate).OrderByDescending(a => a.Read == false).ToList();
            }
            return Json(Lstvm);
        }
        [HttpGet]
        public IActionResult SupportDetails(int ID)
        {
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
            VmTicket vm = new VmTicket();
            vm.ID = qSupport.ID;
            vm.Email = qSupport.TblUser.Email;
            vm.Date = qSupport.Date.ToShamsi().ToString();
            vm.SenderName = qSupport.TblUser.FirstName + " " + qSupport.TblUser.LastName;
            vm.Title = qSupport.Title;
            vm.ID = qSupport.ID;

            vm.ListTicketMsg = new List<TicketMsg>();
            FileRepository RepImg = new FileRepository();
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

                msg.File = new MessageFile();
                if (item.TblFiles != null)
                {
                    msg.File.ID = item.TblFiles.ID;
                    var file = RepImg.GetFileByID(item.TblFiles.ID);
                    msg.File.Link = file.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + file.TblServer.Path.Trim(new char[] { '/' }) + "/" + file.FileName; ;
                    msg.File.Title = item.TblFiles.Title;
                }
                vm.ListTicketMsg.Add(msg);
            }
            _context.SaveChanges();

            ViewData["PageTitle"] = "پشتیبانی";
            ViewData["currentTab"] = "support";
            ViewData["current_subTab"] = "ticket";
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
                    string FileName = "Attached" + DateTime.Now.ToString("yyyyMMddhhmmss") + "." + format;
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
        [Route("Profile/ConvDetails/{ID}")]
        public IActionResult ConvDetails(int ID)
        {
            string userID = User.GetUserID();
            var qChat = _context.TblChat.Where(a => a.ID == ID)
                .Include(a => a.TblUser)
                .Include(a => a.TblMessages).ThenInclude(a => a.TblUser)
                .Include(a => a.TblMessages).ThenInclude(a => a.TblFiles)
                .Include(a => a.TblMessages).ThenInclude(a => a.TblCourse)
                .SingleOrDefault();

            if (qChat.Read == false)
            {
                qChat.Read = true;
                _context.Update(qChat);
            }

            FileRepository RepImg = new FileRepository();
            VmTicket vm = new VmTicket();
            vm.ID = qChat.ID;
            vm.Email = string.IsNullOrEmpty(qChat.TblUser.Email) ? "ثبت نشده" : qChat.TblUser.Email;
            vm.Date = qChat.Date.ToShamsi().ToString("yyyy/MM/dd HH:mm");
            vm.UserName = qChat.TblUser.UserName;
            vm.UserID = qChat.TblUser.Id;
            vm.SenderName = qChat.TblUser.FirstName + " " + qChat.TblUser.LastName;
            vm.Mobile = string.IsNullOrEmpty(qChat.TblUser.Mobile) ? "ثبت نشده" : qChat.TblUser.Mobile;
            vm.CourseName = qChat.TblMessages.FirstOrDefault().TblCourse.Title;

            var UserImage = RepImg.GetImageByID(qChat.TblUser.ImageID);
            vm.ProfileImage = UserImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + UserImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + UserImage.FileName;

            string UserID = User.GetUserID();
            vm.ListTicketMsg = new List<TicketMsg>();
            foreach (var item in qChat.TblMessages.OrderByDescending(a => a.Date))
            {
                if (qChat.TblMessages.Where(a => a.ReaseverID != qChat.UserID).Where(a => a.Read == false).ToList().Count() > 0)
                {
                    item.Read = true;
                    _context.Update(item);
                }
                var chatImage = RepImg.GetImageByID(item.TblUser.ImageID);
                TicketMsg msg = new TicketMsg();
                msg.Text = item.Text;
                msg.SenderID = item.SenderID;
                msg.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd HH:mm");
                msg.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                msg.ProfileImage = chatImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + chatImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + chatImage.FileName;

                if (item.TblFiles != null)
                {
                    string IpAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    var SecurityIp = IpAddress.ConvertScurity();
                    string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("Armis" + "&" + SecurityIp));
                    string SecurityKey = svcCredentials;

                    msg.File = new MessageFile();
                    msg.File.ID = item.TblFiles.ID;
                    var file = RepImg.GetFileByID(item.TblFiles.ID);
                    string path = file.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + file.TblServer.Path.Trim(new char[] { '/' }) + "/" + file.FileName;
                    msg.File.Link = path + "?u=" + SecurityKey;
                    msg.File.Title = item.TblFiles.Title;
                }
                vm.ListTicketMsg.Add(msg);
            }
            _context.SaveChanges();
            ViewBag.CourseID = qChat.TblMessages.FirstOrDefault().CourseID;
            ViewBag.ReaseverID = qChat.UserID;
            ViewBag.ChatID = qChat.ID;
            ViewBag.Level = qChat.TblMessages.FirstOrDefault().Level;

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["currentNav"] = "conv";
            ViewData["breadcrumb"] = "جزئیات گفتگو";
            return View(vm);
        }

        public JsonResult SendConversionMsg(TblMessages t)
        {
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
                    string FileName = "Attached" + DateTime.Now.ToString("yyyyMMddhhmmss") /*+ "." + format*/;
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
                        file.Token = stringGuids;
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

        public IActionResult Students(int Page = 1, string Name = "", string Family = "", int CourseID = 0, int Evi = 0)
        {
            string UserID = User.GetUserID();
            var qCourse = _context.TblUserCourse.Where(a => a.TblCourse.TeacherID == UserID)
                .Include(a => a.TblCourse)
                .Include(a => a.TblUser).OrderByDescending(a => a.Date).AsEnumerable();

            if (!string.IsNullOrEmpty(Name))
            {
                qCourse = qCourse.Where(a => a.TblUser.FirstName.Contains(Name));
            }
            if (!string.IsNullOrEmpty(Family))
            {
                qCourse = qCourse.Where(a => a.TblUser.LastName.Contains(Family));
            }
            if (CourseID > 0)
            {
                qCourse = qCourse.Where(a => a.CourseID == CourseID);
            }

            int Take = 14;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = qCourse.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            List<UsersCourse> lstUser = new List<UsersCourse>();
            TimeUtility time = new TimeUtility();

            if (qCourse.Count() > 0)
            {
                qCourse = qCourse.Skip(Skip).Take(Take);
            }
            foreach (var item in qCourse)
            {
                UsersCourse vm = new UsersCourse();
                vm.ID = item.ID;
                vm.FirstName = item.TblUser.FirstName;
                vm.LastName = item.TblUser.LastName;
                vm.UserName = item.TblUser.UserName;
                vm.LastEntry = time.GetTimeName(item.TblUser.LastEntry);
                vm.Date = item.Date.ToShamsi().ToString("MM/dd/yyyy");
                vm.CourseTitle = item.TblCourse.Title;
                var qLevel = _context.TblUserLevels.Where(a => a.UserID == item.UserID).Where(a => a.CourseID == item.CourseID).OrderBy(a => a.Level).ToList();
                foreach (var itemLevel in qLevel)
                {
                    vm.LevelsList += itemLevel.Level + " , ";
                }
                //vm.Status = item.Status;
                lstUser.Add(vm);
            }

            ViewBag.Name = Name;
            ViewBag.Family = Family;
            ViewBag.CourseID = CourseID;
            ViewBag.Evi = Evi;

            ViewData["Courses"] = new SelectList(_context.TblCourse.Where(a => a.TeacherID == UserID), "ID", "Title");
            ViewData["PageTitle"] = "هنرجویان";
            ViewData["currentTab"] = "reports";
            ViewData["current_subTab"] = "student";
            return View(lstUser);
        }
        [Route("Profile/MyCourses")]
        public IActionResult MyCourses()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["Courses"] = new SelectList(_context.TblCourse.Where(a => a.TeacherID == UserID), "Title", "Title");
            ViewData["currentNav"] = "courses";
            ViewData["breadcrumb"] = "دوره های من";
            return View();
        }
        public JsonResult GetCourses()
        {
            string UserID = User.GetUserID();
            var qCourse = _context.TblCourse.Where(a => a.TeacherID == UserID).Where(a => a.Status == 1 || a.Status == 2)
                .Include(t => t.TblGroup).Include(t => t.TblUser).OrderByDescending(a => a.Date).AsQueryable();

            List<VmCourses> lstCourse = new List<VmCourses>();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qCourse)
            {
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Status = item.Status;
                vm.Link = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd");
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

        [Route("Profile/OnlineClass")]
        public IActionResult OnlineClass()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            //PayRepository Rep_Pay = new PayRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var qClass = _context.TblOnlineCourse.Where(a => a.TeacherID == UserID)
                .Include(a => a.TblOnlineGroup).Include(a=>a.TblOnlineCoursePrice)
                .Include(a=>a.TblUserOnlineCourse)
                .OrderByDescending(a => a.Date);

            // چک کردن اتمام جلسه ها
            //await Rep_Pay.CheckAndPayFinishedSe(_userManager);

            FileRepository RepImage = new FileRepository();
            List<VmClassStudent> LstVm = new List<VmClassStudent>();
            foreach (var item in qClass)
            {
                VmClassStudent vm = new VmClassStudent();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Type = item.Type;
                vm.Link = "/ClassView/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.StuduntCount = item.TblUserOnlineCourse.Count();
                if (item.Type == 2)
                {
                    if (item.TblOnlineCoursePrice != null)
                    {
                        vm.ConfirmedPrice = item.TblOnlineCoursePrice.Confirmed;
                    }
                }

                var cImage = RepImage.GetImageByID(item.ImageID);
                if(cImage != null)
                {
                    vm.Image = cImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + cImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + cImage.FileName;
                }
                else
                {
                    vm.Image = "/assets/media/misc/image2.png";
                }
                LstVm.Add(vm);
            }

            ViewData["currentNav"] = "onlineClass";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View(LstVm);
        }
        [Route("Profile/ClassScheduling/{ID}")]
        public IActionResult ClassScheduling(int ID)
        {
            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewBag.CourseID = ID;
            ViewData["currentNav"] = "onlineClass";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View();
        }

        [HttpGet]
        public JsonResult getEventData(int courseID)
        {
            // page : ClassScheduling
            var qEvents = _context.TblOnlineScheduling.Include(a => a.TblOnlineCourse);
            var qUserEvents = _context.TblUserEvents.Where(a => a.CourseID == courseID).AsQueryable();
            string UserID = User.GetUserID();

            List<VmScheduleEvent> LstVm = new List<VmScheduleEvent>();
            foreach (var item in qEvents.Where(a => a.CourseID == courseID).ToList())
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = item.Title;
                if (qUserEvents.Where(a => a.StartDate == item.StartDate).FirstOrDefault() != null)
                {
                    vm.constraint = "reservedForClass";
                    vm.groupId = "reservedForClass";
                    vm.title = "رزرو";
                    vm.overlap = false;
                    vm.color = "green";
                }
                LstVm.Add(vm);
            }
            foreach (var item in qEvents.Where(a => a.TblOnlineCourse.TeacherID == UserID).Where(a => a.CourseID != courseID).ToList())
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = item.Title;
                vm.constraint = "reservedForClass";
                vm.groupId = "reservedForClass";
                vm.title = "تعیین شده";
                vm.overlap = false;
                vm.color = "grey";
                LstVm.Add(vm);
            }

            return Json(LstVm.ToArray());
        }
        [Route("Profile/ClassBooking/{ID}")]
        public IActionResult ClassBooking(int ID)
        {
            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewBag.CourseID = ID;
            ViewData["currentNav"] = "onlineClass";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View();
        }
        public JsonResult getBookingEventData(int courseID, string userName)
        {
            // Page : ClassBooking
            string UserID = User.GetUserID();
            var qEvents = _context.TblOnlineScheduling.Where(a => a.CourseID == courseID).ToList();
            var qUserEvents = _context.TblUserEvents.Where(a => a.CourseID == courseID).AsQueryable();
            if (!string.IsNullOrEmpty(userName))
            {
                qUserEvents = qUserEvents.Where(a => a.TblUser.UserName == userName);
            }

            List<VmScheduleEvent> LstVm = new List<VmScheduleEvent>();
            bool update = false;

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
            foreach (var item in qUserEvents.ToList())
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = item.Title;
                DateTime end15Minutes = item.EndDate.AddMinutes(15);
                if (DateTime.Now > end15Minutes && item.Status == 0)
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
                if (item.Status == 2 || item.Status == 3)
                {
                    vm.groupId = "cancelClass";
                    vm.title = "لغو";
                    vm.color = item.Status ==2 ? "orange":"red";
                }
                LstVm.Add(vm);
            }
            if (update)
            {
                _context.SaveChanges();
            }
            return Json(LstVm.ToArray());
        }

        [Route("Profile/ClassPrice/{ID}")]
        public IActionResult ClassPrice(int ID)
        {

            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var qPrice = _context.TblOnlineCoursePrice.Where(a => a.CourseID == ID).SingleOrDefault();

            ViewBag.CourseID = ID;
            ViewData["currentNav"] = "onlineClass";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View(qPrice);
        }
        [HttpPost]
        public IActionResult ClassPrice(TblOnlineCoursePrice t)
        {
            try
            {
                var qPrice = _context.TblOnlineCoursePrice.Where(a => a.CourseID == t.CourseID).FirstOrDefault();
                if (qPrice != null)
                {
                    qPrice.NewPrice = t.NewPrice;
                    qPrice.PkgDiscount1 = t.PkgDiscount1;
                    qPrice.PkgDiscount2 = t.PkgDiscount2;
                    qPrice.PkgDiscount3 = t.PkgDiscount3;
                    qPrice.PkgDiscount4 = t.PkgDiscount4;
                    qPrice.PkgDiscount5 = t.PkgDiscount5;
                    qPrice.Confirmed = false;

                    _context.Update(qPrice);
                }
                else
                {
                    t.Confirmed = false;
                    _context.Add(t);
                }
                _context.SaveChanges();

                TempData["Style"] = "success";
                TempData["Message"] = "تعیین قیمت کلاس آموزشی با موفقیت انجام گردید";

            }
            catch (Exception)
            {
                TempData["Style"] = "danger";
                TempData["Message"] = "متاسفانه خطایی رخ داد لطفا با مدیریت در ارتباط باشید";
            }

            return RedirectToAction(nameof(ClassPrice), new { ID = t.CourseID });
        }


        [HttpPost]
        public JsonResult ClassScheduling([FromBody] VmSchedule data)
        {
            if (data.lstEvents != null)
            {
                foreach (var item in data.lstEvents)
                {
                    if (item.id == null)
                    {
                        TblOnlineScheduling t = new TblOnlineScheduling();
                        t.CourseID = data.CourseID;
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
        [Route("Profile/ClassStudents/{ID}")]
        public IActionResult ClassStudents(int ID)
        {
            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);
            bool _updated=false;

            var qStudents = _context.TblUserOnlineCourse.Where(a => a.CourseID == ID).Include(a=>a.TblUser)
                .OrderByDescending(a=>a.Date).ToList();

            var qEvents = _context.TblUserEvents.Where(a=>a.CourseID==ID);

            List<VmClassStudent> LstVm = new List<VmClassStudent>();
            PayRepository RepPay = new PayRepository();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qStudents)
            {
                VmClassStudent vm = new VmClassStudent();
                vm.ID = item.ID;
                vm.UserName = item.TblUser.UserName;
                vm.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd");
                vm.InvoiceID = item.InvoiceID;
                vm.StuduntImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                DateTime nowDate = DateTime.Today;
                int remainingDay = (item.EndDate - nowDate).Days;
                vm.RemainingDay = remainingDay <= 0 ? 0 : remainingDay;

                switch (item.Package)
                {
                    case 1:
                        vm.Package = "پکیج 1 جلسه ای";
                        break;
                    case 2:
                        vm.Package = "پکیج 2 جلسه ای";
                        break;
                    case 4:
                        vm.Package = "پکیج 4 جلسه ای";
                        break;
                    case 8:
                        vm.Package = "پکیج 8 جلسه ای";
                        break;
                    case 12:
                        vm.Package = "پکیج 12 جلسه ای";
                        break;
                }
                var events = qEvents.Where(a => a.InvoiceID == item.InvoiceID);
                if (events.Count() > 0)
                {
                    if (events.Where(a => a.Status == 1 || a.Status == 3 || a.Status == 4).Count() == events.Count() || vm.RemainingDay <= 0)
                    {
                        item.Status = 2;
                        _updated = true;
                        _context.Update(item);
                    }
                }
                
                vm.Status = item.Status;
                LstVm.Add(vm);
            }
            if (_updated)
            {
                _context.SaveChanges();
            }
            ViewBag.CourseID = ID;
            ViewData["currentNav"] = "onlineClass";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View(LstVm);
        }

        [HttpPost]
        public IActionResult getStudentClassEvents(int invoiceID)
        {
            var qUserEvents = _context.TblUserEvents.Where(a => a.InvoiceID == invoiceID).ToList();

            List<VmScheduleEvent> LstVm = new List<VmScheduleEvent>();
            foreach (var item in qUserEvents.ToList())
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.Status = item.Status;
                vm.start = item.StartDate.ToShamsi();
                vm.end = item.EndDate;
                vm.title = item.Title;
                if (item.Status == 2 || item.Status == 3 || vm.start > DateTime.Now)
                {
                    vm.groupId = "locked";
                }
                LstVm.Add(vm);
            }
            return PartialView("P_StudentClassEcvents", LstVm);
        }

        public async Task<JsonResult> setStudentEventStatus(int[] EventID)
        {
            // اتمام پایان جلسه و تسویه حساب

            var qUserEvent = _context.TblUserEvents.Include(a=>a.TblOnlineCourse);

            PayRepository Rep_Pay = new PayRepository();

            var qUserClass = _context.TblUserOnlineCourse.Where(a => a.InvoiceID == qUserEvent.FirstOrDefault().InvoiceID)
                    .Include(a => a.TblBookingInvoice).SingleOrDefault();

            int totalPrice = qUserClass.TblBookingInvoice.Amount;
            int tPercent = qUserClass.TblBookingInvoice.TeacherPercent > 0 ? qUserClass.TblBookingInvoice.TeacherPercent 
                : qUserEvent.FirstOrDefault().TblOnlineCourse.TeacherPercent;

            // حق الزحمه سایت = مبلغ کل - دستمزد استاد
            //int WagePrice = totalPrice - ((totalPrice * tPercent) / 100);
            int basePrice = (totalPrice) / qUserClass.Package;
            int finalPrice = ((basePrice * tPercent) / 100);
            int RefrenceID = 0;
            for (int i = 0; i < EventID.Length; i++)
            {
                var uEvent = qUserEvent.Where(a => a.ID == EventID[i])
                    .Include(a=>a.TblOnlineCourse).ThenInclude(a=>a.TblOnlineCoursePrice)
                    .Include(a=>a.TblUserEventsInvoice).SingleOrDefault();
                if (uEvent.StartDate > DateTime.Now)
                {
                    return Json(new { result="faill", msg="لطفا کلاس های برگزار نشده را انتخاب ننمایید !" });
                }
                if(uEvent!=null && uEvent.Status == 0)
                {
                    //int percentAmount = ((basePrice * tPercent) / 100);
                    if (uEvent.TblUserEventsInvoice == null)
                    {
                        // بدون جریمه
                        TblUserEventsInvoice t = new TblUserEventsInvoice();
                        t.Amount = finalPrice;
                        t.Date = DateTime.Now;
                        t.TeacherID = uEvent.TblOnlineCourse.TeacherID;
                        t.UserEventID = uEvent.ID;
                        _context.Add(t);

                        RefrenceID = t.ID;
                    }
                    else
                    {
                        // با جریمه
                        uEvent.TblUserEventsInvoice.Amount = basePrice - uEvent.TblUserEventsInvoice.AmountOfFines;
                        uEvent.TblUserEventsInvoice.Date = DateTime.Now;

                        // کسر جریمه از سود استاد
                        finalPrice = finalPrice - uEvent.TblUserEventsInvoice.AmountOfFines;
                        uEvent.TblUserEventsInvoice.Amount = finalPrice;

                        RefrenceID = uEvent.TblUserEventsInvoice.ID;

                        #region واریز به حساب هنرجو
                        // هشتاد درصد مبلغ برای هنرجو و / باقیمانه برای سایت
                        int StudentPercentAmount = (uEvent.TblUserEventsInvoice.AmountOfFines * 80) / 100;
                        await Rep_Pay.DepositToStudent(_context, _userManager, qUserClass.UserID,
                            "بازگشت وجه به دلیل لغو جلسه توسط استاد | " + qUserClass.TblOnlineCourse.Title, StudentPercentAmount, uEvent.TblUserEventsInvoice.ID);
                        #endregion
                    }

                    #region واریز به حساب استاد
                    await Rep_Pay.DepositToTeacher(_context,_userManager,uEvent.TblOnlineCourse.TeacherID,
                        "پرداخت وجه بابت اتمام کلاس در  : " + uEvent.TblOnlineCourse.Title, finalPrice, RefrenceID);
                    #endregion

                    uEvent.Status = 1;
                    _context.Update(uEvent);
                }
            }
            _context.SaveChanges();
            return Json(new { result = "ok", msg = "بروزرسانی با موفقیت انجام شد" });
        }

        [Route("Profile/ClassStudentsReceipt/{ID}")]
        public IActionResult ClassStudentsReceipt(int ID)
        {
            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["currentNav"] = "onlineClass";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";

            var qUserEvents = _context.TblUserEvents.Where(a => a.InvoiceID == ID).OrderBy(a=>a.StartDate)
                .Include(a=>a.TblUserEventsInvoice).ToList();
            List<VmTeacherInvoice> LstVm = new List<VmTeacherInvoice>();

            //if (qUserEvents.Count <= 0)
            //{
            //    return View(LstVm);
            //}
            var qUserClass = _context.TblUserOnlineCourse.Where(a => a.InvoiceID == ID)
                .Include(a=>a.TblBookingInvoice).Include(a=>a.TblOnlineCourse).SingleOrDefault();

            int packageNum = qUserClass.Package;
            int tPercent = qUserClass.TblBookingInvoice.TeacherPercent > 0 ? qUserClass.TblBookingInvoice.TeacherPercent :
                qUserClass.TblOnlineCourse.TeacherPercent;

            int TotalPrice = qUserClass.TblBookingInvoice.Amount;

            //int WagePrice= TotalPrice - ((TotalPrice * tPercent) / 100);
            //ViewBag.WagePrice = WagePrice.ToString("N0").ConvertNumerals();

            ViewBag.Des = qUserEvents.Count() + " از " + packageNum;

            int FinalPrice = TotalPrice;

            PayRepository RepPay = new PayRepository();
            LstVm = RepPay.GetPaymentReceipt(LstVm, FinalPrice, packageNum, tPercent, qUserClass, qUserEvents);
            ViewBag.TotalPrice = LstVm.Sum(a => a.TeacherPrice).ToString("N0").ConvertNumerals();

            return View(LstVm);
        }
        public JsonResult ChangeClassStatus(int ID)
        {
            var qUserClass = _context.TblUserOnlineCourse.Where(a => a.ID == ID)
                .Where(a => a.TblOnlineCourse.TeacherID == User.GetUserID()).Include(a => a.TblOnlineCourse).SingleOrDefault();
            if (qUserClass.Status == 0)
            {
                qUserClass.Status = 1;
                _context.Update(qUserClass);
                _context.SaveChanges();
            }
            
            return Json("ok");
        }
        [Route("Profile/StudentBooking/{ID}")]
        public IActionResult StudentBooking(int ID)
        {
            string UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var qStudentClass = _context.TblBookingInvoice.Where(a => a.ID == ID).SingleOrDefault();

            ViewBag.CourseID = qStudentClass.CourseID;
            ViewBag.InvoiceID = qStudentClass.ID;
            ViewData["currentNav"] = "onlineClass";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کلاس های آنلاین";
            return View();
        }
        public JsonResult getStudentBookingEventData(int invoiceID, int CourseID)
        {
            // Page : StudentBooking
            string UserID = User.GetUserID();
            var qUserEvents = _context.TblUserEvents.Where(a => a.InvoiceID == invoiceID).AsQueryable();
            var qEvents = _context.TblOnlineScheduling.Where(a => a.CourseID == CourseID).ToList();

            List<VmScheduleEvent> LstVm = new List<VmScheduleEvent>();
            //bool update = false;

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
            foreach (var item in qUserEvents.ToList())
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = item.Title;

                // اتمام خودکار جلسه بعد از گذشت 15 دقیقه از اتمام زمان
                //DateTime end15Minutes = item.EndDate.AddMinutes(15);
                //if (DateTime.Now > end15Minutes && item.Status == 0)
                //{
                //    item.Status = 1;
                //    update = true;
                //    _context.Update(item);
                //}
                if (item.Status == 1)
                {
                    vm.title = "اتمام";
                    vm.color = "green";
                }
                if (item.Status == 2 || item.Status == 3)
                {
                    vm.groupId = "cancelClass";
                    vm.title = "لغو";
                    vm.color = item.Status == 2 ? "orange" : "red";
                }
                LstVm.Add(vm);
            }
            //if (update)
            //{
            //    _context.SaveChanges();
            //}
            return Json(LstVm.ToArray());
        }
        [HttpPost]
        public JsonResult deleteEventData(int ID)
        {
            try
            {
                var qEvent = _context.TblOnlineScheduling.Where(a => a.ID == ID).SingleOrDefault();

                var qUserEvents = _context.TblUserEvents.Where(a => a.StartDate >= qEvent.StartDate && a.StartDate <= qEvent.EndDate).ToList();
                if (qUserEvents.Count > 0)
                {
                    return Json(new { result = "error", msg = "هم اکنون جلسه ای برای این زمان بندی رزرو شده و امکان حذف نمی باشد" });
                }
                else
                {
                    _context.Remove(qEvent);
                    _context.SaveChanges();
                }
                return Json(new { result = "ok", msg = "زمان بندی با موفقیت حذف گردید" });
            }
            catch (Exception)
            {
                return Json(new {result="error",msg="متاسفانه خطایی رخ داد" });
            }
        }
        [HttpPost]
        public JsonResult cancelEventData(string ID)
        {
            try
            {
                var qEvent = _context.TblUserEvents.Where(a => a.ID == Convert.ToInt32(ID))
                    .Include(a=>a.TblOnlineCourse).ThenInclude(a=>a.TblOnlineCoursePrice).SingleOrDefault();

                if (qEvent.Status == 1 || qEvent.StartDate < DateTime.Now)
                {
                    return Json(new {result="error", msg="امکان لغو کلاس های برگزار شده وجود ندارد" });
                }
                if (qEvent.Status == 2)
                {
                    return Json(new { result = "error", msg = "امکان حذف کلاس های لغو شده وجود ندارد" });
                }
                var qUserClass = _context.TblUserOnlineCourse.Where(a => a.InvoiceID == qEvent.InvoiceID)
                .Include(a => a.TblBookingInvoice).SingleOrDefault();

                int dur_4 = 4;
                int dur_17 = 17;
                int percent = 0;
                int totalPrice = qUserClass.TblBookingInvoice.Amount;
                int tPercent = qUserClass.TblBookingInvoice.TeacherPercent > 0 ? qUserClass.TblBookingInvoice.TeacherPercent : qEvent.TblOnlineCourse.TeacherPercent;
                // حق الزحمه سایت = مبلغ کل - دستمزد استاد
                //int WagePrice = totalPrice - ((totalPrice * tPercent) / 100);
                // 

                int basePrice = totalPrice / qUserClass.Package;
                int percentAmount = 0;

                if(qEvent.StartDate < DateTime.Now.AddHours(dur_17))
                {
                    TblUserEventsInvoice t = new TblUserEventsInvoice();
                    t.Date = DateTime.Now;
                    if (qEvent.StartDate <= DateTime.Now.AddHours(dur_4))
                    {
                        // لغو در بازه 4 ساعته
                        // کسر 30 درصد
                        percent = 30;
                        percentAmount = ((basePrice * percent) / 100);
                        t.AmountOfFines += percentAmount;
                    }
                    else if (qEvent.StartDate <= DateTime.Now.AddHours(dur_17))
                    {
                        // لغو در بازه 17 ساعته
                        // کسر 20 درصد
                        percent = 20;
                        percentAmount = ((basePrice * percent) / 100);
                        t.AmountOfFines += percentAmount;
                    }
                    t.Description = "کسر "+ percent+" "+ " درصد بابت لغو جلسه در تاریخ" + t.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                    t.TeacherID = qEvent.TblOnlineCourse.TeacherID;
                    t.UserEventID = qEvent.ID;
                    _context.Add(t);
                }

                qEvent.Status = 2;
                _context.Update(qEvent);
                _context.SaveChanges();
                return Json(new { result = "ok", msg = "جلسه مورد نظر با موفقیت لغو گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "error", msg = "خطا در ارسال لطفا با مدیریت در تماس باشید" });
            }
        }

        [Route("Profile/MyStudents")]
        public IActionResult MyStudents()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["Courses"] = new SelectList(_context.TblCourse.Where(a => a.TeacherID == UserID), "Title", "Title");
            ViewData["currentNav"] = "students";
            ViewData["breadcrumb"] = "هنرجویان من";
            return View();
        }
        public JsonResult GetStudents()
        {
            string UserID = User.GetUserID();
            var qCourse = _context.TblUserCourse.Where(a => a.TblCourse.TeacherID == UserID)
                .Include(a => a.TblCourse)
                .Include(a => a.TblUser).OrderByDescending(a => a.Date).AsEnumerable();

            List<UsersCourse> lstUser = new List<UsersCourse>();
            FileRepository RepImg = new FileRepository();
            TimeUtility time = new TimeUtility();
            foreach (var item in qCourse)
            {
                UsersCourse vm = new UsersCourse();
                vm.ID = item.ID;
                vm.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.UserName = item.TblUser.UserName;
                //vm.LastEntry = time.GetTimeName(item.TblUser.LastEntry);
                vm.Date = item.Date.ToShamsi().ToString("MM/dd/yyyy");
                vm.CourseTitle = item.TblCourse.Title;
                var qFullLevels = _context.TblUserCourse
                    .Where(a => a.UserID == item.UserID).Where(a => a.CourseID == item.CourseID)
                    .Where(a => a.LevelID == 1000).FirstOrDefault();
                if (qFullLevels == null)
                {
                    var qLevel = _context.TblUserLevels.Where(a => a.UserID == item.UserID).Where(a => a.CourseID == item.CourseID).OrderBy(a => a.Level).ToList();
                    foreach (var itemLevel in qLevel)
                    {
                        vm.LevelsList += itemLevel.Level + " , ";
                    }
                }
                else
                {
                    vm.LevelsList = "full";
                }
                var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                lstUser.Add(vm);
            }
            return Json(lstUser);
        }
        public IActionResult CourseFinancial(int Page = 1, string Name = "", string Family = "", int Type = 0, int CourseID = 0)
        {
            string UserID = User.GetUserID();
            var qPayment = _context.TblTransaction.Where(a => a.ToUserID == UserID || a.TblInvoice.TblCourse.TeacherID == UserID)
                .Include(a => a.TblUser)
                .Include(a => a.TblInvoice).ThenInclude(a => a.TblCourse)
                .OrderByDescending(a => a.Date).AsQueryable();

            if (!string.IsNullOrEmpty(Name))
            {
                qPayment = qPayment.Where(a => a.TblUser.FirstName.Contains(Name));
            }
            if (!string.IsNullOrEmpty(Family))
            {
                qPayment = qPayment.Where(a => a.TblUser.LastName.Contains(Family));
            }
            if (Type > 0)
            {
                qPayment = qPayment.Where(a => a.Type == Type);
            }
            if (CourseID > 0)
            {
                qPayment = qPayment.Where(a => a.TblInvoice.CourseID == CourseID);
            }
            int Take = 14;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = qPayment.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            ViewData["Courses"] = new SelectList(_context.TblCourse.Where(a => a.TeacherID == UserID), "ID", "Title");
            ViewData["PageTitle"] = "مالی";
            ViewData["currentTab"] = "reports";
            ViewData["current_subTab"] = "financial";

            if (qPayment.Count() > 0)
            {
                qPayment = qPayment.Skip(Skip).Take(Take);
            }
            return View(qPayment.ToList());
        }
        [Route("Profile/CourseFinancial")]
        public IActionResult CourseFinancial()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            //ViewData["Courses"] = new SelectList(_context.TblCourse.Where(a => a.TeacherID == UserID), "ID", "Title");
            ViewData["currentNav"] = "financial";
            ViewData["breadcrumb"] = "گزارش مالی";
            return View();
        }
        public JsonResult GetFinancial()
        {
            string UserID = User.GetUserID();
            var qPayment = _context.TblTransaction.Where(a => a.ToUserID == UserID || a.TblInvoice.TblCourse.TeacherID == UserID)
                .Include(a => a.TblUser)
                .Include(a => a.TblInvoice).ThenInclude(a => a.TblCourse)
                .OrderByDescending(a => a.Date).AsQueryable();

            List<VmInvoice> Lstvm = new List<VmInvoice>();
            foreach (var item in qPayment)
            {
                VmInvoice vm = new VmInvoice();
                vm.ID = item.ID;
                vm.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Description = item.Description;
                vm.Type = item.Type;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                if (item.Type == 3 || item.Type == 4)
                {
                    vm.Amount = item.Amount;
                }
                else
                {
                    #region سود معرف
                    int Benefits = 0;
                    var qEarnReagent = _context.TblEarnReagent.Where(a => a.InviceID == item.InvioceID).FirstOrDefault();
                    if (qEarnReagent != null)
                    {
                        Benefits = qEarnReagent.Amount;
                    }
                    #endregion
                    int finalPrice = item.TblInvoice.Amount - Benefits;

                    vm.Amount = (finalPrice * item.TblInvoice.TblCourse.PercentSale) / 100;
                    
                }
                vm.NewInventory= item.NewInventory==null ?0: (int)item.NewInventory;
                vm.BoonID = 0;
                if (item.TblInvoice != null && item.TblInvoice.BoonID != null)
                {
                    vm.BoonID = (int)item.TblInvoice.BoonID;
                }
                Lstvm.Add(vm);
            }
            return Json(Lstvm);
        }
        public JsonResult GetUserCourses(string ID)
        {
            var qUserCourse = _context.TblUserCourse.Where(a => a.UserID == ID)
                .Include(a => a.TblCourse)
                .ToList();
            List<VmSelect> lstSelect = new List<VmSelect>();
            foreach (var item in qUserCourse)
            {
                VmSelect vm = new VmSelect();
                vm.ID = item.TblCourse.ID;
                vm.Title = item.TblCourse.Title;
                lstSelect.Add(vm);
            }
            return Json(new { model = lstSelect });
        }
        [Route("Profile/Placement")]
        public IActionResult Placement()
        {
            string UserID = User.GetUserID();

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["currentNav"] = "placement";
            ViewData["breadcrumb"] = "تعیین سطح";
            return View();
        }
        public JsonResult GetPlacement()
        {
            string UserID = User.GetUserID();
            var qPalcement = _context.TblPlacement.Where(a => a.TeacherID == UserID)
                .Include(a => a.TblUser).ThenInclude(a => a.TblUserSocial)
                .OrderByDescending(a => a.ID).AsQueryable();


            List<VmPlacement> Lstvm = new List<VmPlacement>();
            foreach (var item in qPalcement)
            {
                VmPlacement vm = new VmPlacement();
                vm.ID = item.ID;
                vm.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.SheetLevel = item.SheetLevel;
                vm.TheoreticalLevel = item.TheoreticalLevel;
                vm.Status = item.Status;
                vm.TeacherDescription = item.TeacherDescription;
                vm.StudentDescription = item.StudentDescription;
                vm.TrainingPeriod = item.TrainingPeriod;
                vm.TelegramID = item.TblUser.TblUserSocial == null ? "ثبت نشده" : item.TblUser.TblUserSocial.TelegramID;
                vm.SkypeID = item.TblUser.TblUserSocial == null ? "ثبت نشده" : item.TblUser.TblUserSocial.SkypeID;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd");
                Lstvm.Add(vm);
            }
            return Json(Lstvm);
        }
        [HttpPost]
        public IActionResult Placement(TblPlacement t)
        {
            try
            {
                var qPlacement = _context.TblPlacement.Where(a => a.ID == t.ID).SingleOrDefault();

                qPlacement.Status = t.Status;
                qPlacement.TeacherDescription = t.TeacherDescription;
                _context.Update(qPlacement);
                _context.SaveChanges();
                return Json(new { result = "ok", msg = "عملیات با موفقیت انجام شد" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در بروزرسانی. لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        [HttpPost]
        public IActionResult EditBankDetails(int ID, string BankName, string CardNumber, string ShabaNumber, string AccountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(BankName) || string.IsNullOrEmpty(CardNumber) || string.IsNullOrEmpty(ShabaNumber) || string.IsNullOrEmpty(AccountNumber))
                {
                    return Json("notValid");
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
                return Json("faill");
            }
        }
        [HttpPost]
        public JsonResult GetBoonDetails(int BoonID)
        {
            try
            {
                var qBoon = _context.TblBoon.Where(a => a.ID == BoonID).SingleOrDefault();
                string Percent = qBoon.Percent + "%";

                return Json(new { result = "ok", title = qBoon.Title, percent = Percent, count = qBoon.Count });
            }
            catch (Exception)
            {
                return Json("");
            }
        }
        public JsonResult UploadTeacherDocs(int Sort)
        {
            return Json("");
        }
        [HttpPost]
        public IActionResult DelBankDetails(int ID)
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
        public IActionResult AllComments(int Page = 1)
        {
            string UserID = User.GetUserID();
            var qComment = _context.TblComment.Where(a => a.UserReciver == UserID)
                .Include(a => a.TblUserSender)
                .Include(a => a.TblCourse)
                .OrderByDescending(a => a.Date);
            int Take = 18;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = qComment.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            List<SessionComments> vm = new List<SessionComments>();
            TimeUtility time = new TimeUtility();
            //FileRepository RepImg = new FileRepository();
            foreach (var item in qComment.Skip(Skip).Take(Take))
            {
                SessionComments sc = new SessionComments();
                sc.ID = item.ID;
                sc.FullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                sc.Text = sc.FullName + " دیدگاه جدیدی را در " + item.TblCourse.Title + " ارسال کرده است ";
                sc.Link = "/Home/CourseView/" + item.TblCourse.ShortLink.Replace(" ", "-");
                sc.Date = time.GetDateName(item.Date);
                //var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                //sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                vm.Add(sc);
            }
            ViewData["PageTitle"] = "دیدگاه ها";
            ViewData["currentTab"] = "home";
            ViewData["current_subTab"] = "home";
            return View(vm);
        }
        public IActionResult AllNotics(int Page = 1)
        {
            string UserID = User.GetUserID();
            var qNotic = _context.TblMessages.Where(a => a.ReaseverID == UserID)
                .Include(a => a.TblUser)
                .Include(a => a.TblCourse);

            var qMsgTicket = _context.TblTicketMsg.Where(a => a.ReceiverID == UserID).Where(a => a.TblTicket.Section != 5)
                .Include(a => a.TblTicket)
                .Include(a => a.TblUserSender);

            List<VmMessage> LstNotic = new List<VmMessage>();
            //FileRepository RepImg = new FileRepository();
            TimeUtility time = new TimeUtility();
            foreach (var item in qNotic)
            {
                VmMessage vm = new VmMessage();
                vm.ID = item.ID;
                vm.Read = item.Read;
                vm.SenderName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Text = vm.SenderName + " پیامی را ارسال کرده است";
                vm.Link = "/Course/" + item.TblCourse.ShortLink.Replace(" ", "-") + "/" + item.CourseID;
                vm.Date = time.GetDateName(item.Date);
                vm.DateName = time.GetTimeName(item.Date);
                vm.Time = item.Date.ToString("hh:mm");
                //var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                //vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                LstNotic.Add(vm);
            }
            foreach (var item in qMsgTicket)
            {
                VmMessage vm = new VmMessage();
                vm.ID = item.ID;
                vm.Read = item.Read;
                vm.SenderName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                vm.Text = vm.SenderName + " پاسخ جدیدی را در تیکت " + item.TblTicket.Title + " ارسال کرده است ";
                vm.Link = "/Teacher/SupportDetails/" + item.TicketID;
                vm.Date = time.GetDateName(item.Date);
                vm.DateName = time.GetTimeName(item.Date);
                vm.Time = item.Date.ToString("hh:mm");
                //var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                //vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                LstNotic.Add(vm);
            }

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

            ViewData["PageTitle"] = "اعلان ها";
            ViewData["currentTab"] = "home";
            ViewData["current_subTab"] = "home";
            return View(Notics);
        }
    }
}
