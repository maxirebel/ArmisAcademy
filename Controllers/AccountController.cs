using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ArmisApp.Models.AccountViewModels;
using ArmisApp.Services;
using ArmisApp.Models.Identity;
using Microsoft.AspNetCore.Http;
using ArmisApp.Models.Domain.context;
using System.IO;
using System.Text.Encodings.Web;
using ArmisApp.Models.ViewModels;
using ArmisApp.Models.Repository;
using ArmisApp.Models.Domain.db;
using System.Text.RegularExpressions;
using System.Net.Mail;
using ArmisApp.Models.Utility;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using ArmisApp.Models.ExMethod;
using System.Net;
using Microsoft.Extensions.Hosting;
using Shyjus.BrowserDetection;

namespace ArmisApp.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly IHostEnvironment _appEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly IHttpContextAccessor _accessor;
        private readonly DataContext _context;
        private readonly IBrowserDetector _browserDetector;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender,
            ILogger<AccountController> logger,
            IHttpContextAccessor accessor,
            IHostEnvironment environment,
            IBrowserDetector browserDetector,
            DataContext context)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _accessor = accessor;
            _context = context;
            _browserDetector = browserDetector;
            _appEnvironment = environment;
        }
        [AllowAnonymous]
        public IActionResult Index()
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToLocal("/" + User.GetRole());
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }
        public async Task<IActionResult> Test()
        {
            var user = await _userManager.FindByNameAsync("Dereny");
            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);

            string EmailTemplate = "";
            using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
            {
                EmailTemplate = reader.ReadToEnd();
            }
            string text = "جهت فعالسازی حساب کاربری خود بر روی لینک زیر کلیک نمایید";
            string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                .Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl)).Replace("[LINK-TITLE]", "تایید حساب");
            await _emailSender.SendEmailConfirmationAsync(user.Email, callbackUrl, Body);

            return Content("OK");
        }
        [TempData]
        public string ErrorMessage { get; set; }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToLocal("/" + User.GetRole());
            }
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> Login(LoginViewModel model, string returnUrl = null)
        {
            model.UserName = model.UserName.ToString().ConvertToStandardNumeral();
            model.Password = model.Password.ToString().ConvertToStandardNumeral();

            ViewData["ReturnUrl"] = returnUrl;
            if (ModelState.IsValid)
            {
                #region بررسی اطلاعات ورودی
                string userName = "";
                if (IsValidEmail(model.UserName))
                {
                    var qUser = await _userManager.FindByEmailAsync(model.UserName);
                    if (qUser == null)
                    {
                        return Json(new { result = "error", response = "ایمیل وارد شده در سیستم وجود ندارد" });
                    }
                    userName = qUser.UserName;
                }
                else if (IsPhoneNumber(model.UserName))
                {

                    var qUser = _context.Users.Where(a => a.Mobile == model.UserName).FirstOrDefault();
                    if (qUser == null)
                    {
                        return Json(new { result = "faill", msg = "تلفن همراه وارد شده در سیستم وجود ندارد" });
                    }
                    userName = qUser.UserName;
                }
                else
                {
                    userName = model.UserName;
                }
                #endregion
                var user = await _userManager.FindByNameAsync(userName);

                // This doesn't count login failures towards account lockout
                // To enable password failures to trigger account lockout, set lockoutOnFailure: true
                var result = await _signInManager.PasswordSignInAsync(userName, model.Password, model.RememberMe, lockoutOnFailure: false);
                if (result.Succeeded)
                {
                    if (user.Status == 0)
                    {
                        await _signInManager.SignOutAsync();
                        _logger.LogInformation("User logged out.");
                        return Json(new { result = "warning", route = "/Account/verification?user=" + user.Id });
                    }
                    if (user.Status == 2)
                    {
                        await _signInManager.SignOutAsync();
                        _logger.LogInformation("User logged out.");
                        return Json(new { result = "error", response = "حساب کاربری مسدود می باشد" });
                    }
                    _logger.LogInformation("User logged in.");

                    user.LastEntry = DateTime.Now;
                    user.IsSignedIn = true;
                    user.IPAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    await _userManager.UpdateAsync(user);
                    string Url = "";
                    if (returnUrl != null)
                    {
                        Url = returnUrl;
                    }
                    else
                    {
                        Url = "/";
                    }
                    #region ثبت فعالیت های اخیر کاربر
                    var qUserActivity = _context.TblUserActivity.Where(a => a.UserID == user.Id).AsQueryable();
                    if (qUserActivity.Count() <= 4)
                    {
                        TblUserActivity t = new TblUserActivity();
                        t.IpAddress = user.IPAddress;
                        t.SystemName = /*Dns.GetHostName()*/System.Runtime.InteropServices.RuntimeInformation.OSDescription;
                        t.UserID = user.Id;
                        //var userAgent = Request.Headers["User-Agent"];
                        var browser = _browserDetector.Browser;
                        t.BrowserName = "BrowserName : "+ browser.Name+ " | BrowserVersion : "+ browser.Version+ " | DeviceType : " + browser.DeviceType;
                        t.Date = DateTime.Now;
                        _context.Add(t);
                        _context.SaveChanges();
                    }
                    else
                    {
                        var qUpdateActivity = qUserActivity.OrderBy(a => a.Date).FirstOrDefault();
                        qUpdateActivity.IpAddress = user.IPAddress;
                        qUpdateActivity.SystemName = Dns.GetHostName();
                        var browser = _browserDetector.Browser;
                        qUpdateActivity.BrowserName = "BrowserName : " + browser.Name + " | BrowserVersion : " + browser.Version + " | DeviceType : " + browser.DeviceType;
                        qUpdateActivity.Date = DateTime.Now;
                        _context.Update(qUpdateActivity);
                        _context.SaveChanges();
                    }
                    #endregion
                    return Json(new { result = "ok", route = Url });
                }
                if (result.IsNotAllowed)
                {
                    return Json(new { result = "warning", route = "/Account/NotConfirm?user=" + user.Id });
                }
                //if (result.RequiresTwoFactor)
                //{
                //    return RedirectToAction(nameof(LoginWith2fa), new { returnUrl, model.RememberMe });
                //}
                //if (result.IsLockedOut)
                //{
                //    _logger.LogWarning("User account locked out.");
                //    return RedirectToAction(nameof(Lockout));
                //}
                else
                {
                    ModelState.AddModelError(string.Empty, "Invalid login attempt.");
                    return Json(new { result = "error", response = "نام کاربری و یا کلمه عبور اشتباه است" });
                }
            }
            return Json(new { result = "error", response = "خطای سیستم . لطفا در زمان دیگری تلاش نمایید" });
        }

        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            string redirectUrl = Url.Action("GoogleResponse", "Account");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return new ChallengeResult("Google", properties);
        }
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
            string[] userInfo = { info.Principal.FindFirst(ClaimTypes.Name).Value, info.Principal.FindFirst(ClaimTypes.Email).Value };

            var qUser = await _userManager.FindByEmailAsync(info.Principal.FindFirst(ClaimTypes.Email).Value);

            if (result.Succeeded)
            return View(userInfo);
            if (qUser != null)
            {
                await _signInManager.SignInAsync(qUser, isPersistent: false);
                return Redirect("/");
            }
            else
            {
                // ایجاد نام کاربری اختصاصی
                Random Rnd = new Random();
                string newUserName = "";
                newUserName = DateTime.Now.ToString("ddhhmm") + Rnd.Next(100, 999);

                ApplicationUser user = new ApplicationUser
                {
                    FirstName= info.Principal.FindFirst(ClaimTypes.Name).Value,
                    LastName = "",
                    Mobile= info.Principal.FindFirst(ClaimTypes.MobilePhone).Value,
                    Email = info.Principal.FindFirst(ClaimTypes.Email).Value,
                    UserName = "User" + newUserName,
                    ReagentCode = newUserName,
                    Status = 1,
                    IPAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString(),
                    LastEntry = DateTime.Now,
                    Date = DateTime.Now,
                    ImageID = 2
                };

                IdentityResult identResult = await _userManager.CreateAsync(user);
                if (identResult.Succeeded)
                {
                    identResult = await _userManager.AddLoginAsync(user, info);
                    if (identResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, false);
                        return View(userInfo);
                    }
                }
                return RedirectToAction(nameof(Login));
            }
        }
        [AllowAnonymous]
        public IActionResult MicrosoftLogin()
        {
            string redirectUrl = Url.Action("MicrosoftResponse", "Account");
            var properties = _signInManager.ConfigureExternalAuthenticationProperties("Microsoft", redirectUrl);
            return new ChallengeResult("Microsoft", properties);
        }
        [AllowAnonymous]
        public async Task<IActionResult> MicrosoftResponse()
        {
            ExternalLoginInfo info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return RedirectToAction(nameof(Login));

            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, false);
            string[] userInfo = { info.Principal.FindFirst(ClaimTypes.Name).Value, info.Principal.FindFirst(ClaimTypes.Email).Value };

            var qUser = await _userManager.FindByEmailAsync(info.Principal.FindFirst(ClaimTypes.Email).Value);

            if (result.Succeeded)
                return View(userInfo);
            if (qUser != null)
            {
                await _signInManager.SignInAsync(qUser, isPersistent: false);
                return Redirect("/");
            }
            else
            {
                // ایجاد نام کاربری اختصاصی
                Random Rnd = new Random();
                string newUserName = "";
                newUserName = DateTime.Now.ToString("ddhhmm") + Rnd.Next(100, 999);

                ApplicationUser user = new ApplicationUser
                {
                    FirstName = info.Principal.FindFirst(ClaimTypes.Name).Value,
                    LastName = "",
                    Mobile = info.Principal.FindFirst(ClaimTypes.MobilePhone).Value,
                    Email = info.Principal.FindFirst(ClaimTypes.Email).Value,
                    UserName = "User" + newUserName,
                    ReagentCode = newUserName,
                    Status = 1,
                    IPAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString(),
                    LastEntry = DateTime.Now,
                    Date = DateTime.Now,
                    ImageID = 2
                };

                IdentityResult identResult = await _userManager.CreateAsync(user);
                if (identResult.Succeeded)
                {
                    identResult = await _userManager.AddLoginAsync(user, info);
                    if (identResult.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, false);
                        return View(userInfo);
                    }
                }
                return RedirectToAction(nameof(Login));
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Signup(string returnUrl = null)
        {
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToLocal("/" + User.GetRole());
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model, int Agree, string returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            try
            {
                if (ModelState.IsValid)
                {
                    model.UserName = model.UserName.ToString().ConvertToStandardNumeral();
                    model.Password = model.Password.ToString().ConvertToStandardNumeral();
                    //var qUser = _userManager.FindByNameAsync(model.UserName).Result;

                    if (IsValidEmail(model.UserName))
                    {
                        model.Email = model.UserName;
                        model.Mobile = "";
                        var qEmail = await _userManager.FindByEmailAsync(model.Email);
                        if (qEmail != null)
                        {
                            return Json(new { result = "faill", msg = "ایمیل وارد شده تکراری می باشد" });
                        }
                    }
                    else if (IsPhoneNumber(model.UserName))
                    {
                        model.Mobile = model.UserName;
                        model.Email = "";
                        var qMobile = _context.Users.Where(a => a.Mobile == model.Mobile).FirstOrDefault();
                        if (qMobile != null)
                        {
                            return Json(new { result = "faill", msg = "تلفن همراه وارد شده تکراری می باشد" });
                        }
                    }
                    else
                    {
                        return Json(new { result = "faill", msg = "تلفن همراه و یا ایمیل وارد شده نامعتبر می باشد" });
                    }
                    if (Agree != 1)
                    {
                        return Json(new { result = "faill", msg = "پذیرفتن قوانین و مقررات ضروری می باشد" });
                    }
                    //if (model.Mobile.Length != 11)
                    //{
                    //    return Json(new { result = "faill", msg = "تلفن همراه وارد شده نامعتبر می باشد" });
                    //}
                    if (model.Password.Length < 6)
                    {
                        return Json(new { result = "faill", msg = "کلمه عبور باید بیشتر از 6 کاراکتر باشد" });
                    }
                    //if (qUser != null)
                    //{
                    //    return Json(new { result = "faill", msg = "نام کاربری وارد شده تکراری می باشد" });
                    //}
                    if (model.Password != model.ConfirmPassword)
                    {
                        return Json(new { result = "faill", msg = "کلمه های عبور با هم مطابقت ندارند" });
                    }
                    Random Rnd = new Random();
                    // ایجاد نام کاربری اختصاصی
                    string newUserName = "";
                    newUserName = DateTime.Now.ToString("ddhhmm") + Rnd.Next(100, 999);
                    //
                    var user = new ApplicationUser
                    {
                        UserName = "User" + newUserName,
                        ReagentCode = newUserName,
                        Email = model.Email,
                        Status = 0,
                        FirstName = model.FirstName,
                        LastName = model.LastName,
                        Mobile = model.Mobile,
                        IPAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString(),
                        LastEntry = DateTime.Now,
                        Date = DateTime.Now,
                        ImageID = 2
                    };
                    var result = await _userManager.CreateAsync(user, model.Password);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation("User created a new account with password.");

                        await _userManager.AddToRoleAsync(user, "Student");
                        await _signInManager.SignInAsync(user, isPersistent: false);

                        var qScore = _context.TblScore;

                        // امتیاز عضویت به کاربر
                        var qJoinScore = qScore.Where(a => a.TitleEn == "newJoin").FirstOrDefault();
                        TblUserScore t = new TblUserScore();
                        t.UserID = user.Id;
                        t.ScoreID = qJoinScore.ID;
                        _context.Add(t);
                        _context.SaveChanges();

                        user.Score += qJoinScore.Value;

                        await _userManager.UpdateAsync(user);

                        if (!string.IsNullOrEmpty(model.Email))
                        {
                            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);

                            string EmailTemplate = "";
                            using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                            {
                                EmailTemplate = reader.ReadToEnd();
                            }
                            string text = "جهت فعالسازی حساب کاربری خود بر روی لینک زیر کلیک نمایید";
                            string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                                .Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl)).Replace("[LINK-TITLE]", "تایید حساب");
                            await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl, Body);
                        }
                        if (!string.IsNullOrEmpty(model.Mobile))
                        {
                            int token = Rnd.Next(1000, 9999);
                            SmsSender sms = new SmsSender();
                            user.MobileConfirmCode = token.ToString() + "," + user.Mobile;
                            await _userManager.UpdateAsync(user);
                            string smsText = "به خانواده بزرگ آرمیس خوش آمدید. کد فعالسازی حساب شما : " + token.ToString();
                            string mCodeSender = sms.SendSms(smsText, model.Mobile);
                        }
                        //#region Notices To Admin
                        //string AdminEmail = "krm.insert@gmail.com";

                        //string EmailTemplate2 = "";
                        //string Link = "https://armisacademy.com/Admin/SupportDetails/" + t.ID;
                        //using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "Email2.html")))
                        //{
                        //    EmailTemplate2 = reader.ReadToEnd();
                        //}
                        //string text2 = "جهت مشاهده کابران بر روی مشاهده کلیک نمایید";
                        //string Body2 = EmailTemplate2.Replace("[TITLE]", "عضویت جدید در آرمیس آکادمی").Replace("[TEXT]", text2)
                        //    .Replace("[LINK]", HtmlEncoder.Default.Encode(Link)).Replace("[LINK-TITLE]", "مشاهده");
                        //await _emailSender.SendEmailConfirmationAsync(AdminEmail, "عضویت جدید در آرمیس آکادمی", Body2);
                        //#endregion

                        _logger.LogInformation("User created a new account with password.");
                        if (returnUrl != null)
                        {
                            // مربوط به فرم ثبت نام در قسمت خرید
                            return Json(new { result = "ok", userID=user.Id, email=model.Email,mobile=model.Mobile });
                        }

                        return Json(new { result = "ok", msg = "/Account/verification?user=" + user.Id });
                    }
                    AddErrors(result);
                }
                // If we got this far, something failed, redisplay form
                return Json(new { result = "faill", msg = "لطفا تمامی فیلد های ضروری را تکمیل نمایید" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال . لطفا در زمان دیگری تلاش نمایید" });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> BecomingTeacher(RegisterViewModel model, int Agree)
        {
            try
            {
                //var qUser = _userManager.FindByNameAsync(model.UserName).Result;

                if (!string.IsNullOrEmpty(model.Email) && IsValidEmail(model.Email))
                {
                    model.Mobile = "";
                    var qEmail = await _userManager.FindByEmailAsync(model.Email);
                    if (qEmail != null)
                    {
                        return Json(new { result = "faill", msg = "ایمیل وارد شده تکراری می باشد" });
                    }
                }
                if (IsPhoneNumber(model.Mobile))
                {
                    model.Email = "";
                    var qMobile = _context.Users.Where(a => a.Mobile == model.Mobile).FirstOrDefault();
                    if (qMobile != null)
                    {
                        return Json(new { result = "faill", msg = "تلفن همراه وارد شده تکراری می باشد" });
                    }
                }
                else
                {
                    return Json(new { result = "faill", msg = "تلفن همراه وارد شده نامعتبر می باشد" });
                }
                if (Agree != 1)
                {
                    return Json(new { result = "faill", msg = "پذیرفتن قوانین و مقررات ضروری می باشد" });
                }
                if (model.Password.Length < 6)
                {
                    return Json(new { result = "faill", msg = "کلمه عبور باید بیشتر از 6 کاراکتر باشد" });
                }
                if (model.Password != model.ConfirmPassword)
                {
                    return Json(new { result = "faill", msg = "کلمه های عبور با هم مطابقت ندارند" });
                }
                Random Rnd = new Random();
                // ایجاد نام کاربری اختصاصی
                string newUserName = "";
                newUserName = DateTime.Now.ToString("ddhhmm") + Rnd.Next(100, 999);
                //
                var user = new ApplicationUser
                {
                    UserName = "User" + newUserName,
                    ReagentCode = newUserName,
                    Email = model.Email,
                    Status = 0,
                    FirstName = model.FirstName,
                    LastName = model.LastName,
                    Mobile = model.Mobile,
                    Description = model.Description,
                    IPAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString(),
                    LastEntry = DateTime.Now,
                    Date = DateTime.Now,
                    ImageID = 2
                };
                var result = await _userManager.CreateAsync(user, model.Password);
                if (result.Succeeded)
                {
                    _logger.LogInformation("User created a new account with password.");

                    await _userManager.AddToRoleAsync(user, "Student");
                    await _signInManager.SignInAsync(user, isPersistent: false);

                    var qScore = _context.TblScore;

                    // امتیاز عضویت به کاربر
                    var qJoinScore = qScore.Where(a => a.TitleEn == "newJoin").FirstOrDefault();
                    TblUserScore t = new TblUserScore();
                    t.UserID = user.Id;
                    t.ScoreID = qJoinScore.ID;
                    _context.Add(t);
                    _context.SaveChanges();

                    user.Score += qJoinScore.Value;

                    await _userManager.UpdateAsync(user);

                    #region SendConfirmNotic
                    if (!string.IsNullOrEmpty(model.Email))
                    {
                        var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                        var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);

                        string EmailTemplate = "";
                        using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                        {
                            EmailTemplate = reader.ReadToEnd();
                        }
                        string text = "جهت فعالسازی حساب کاربری خود بر روی لینک زیر کلیک نمایید";
                        string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                            .Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl)).Replace("[LINK-TITLE]", "تایید حساب");
                        await _emailSender.SendEmailConfirmationAsync(model.Email, callbackUrl, Body);
                    }
                    if (!string.IsNullOrEmpty(model.Mobile))
                    {
                        int token = Rnd.Next(1000, 9999);
                        SmsSender sms = new SmsSender();
                        user.MobileConfirmCode = token.ToString() + "," + user.Mobile;
                        await _userManager.UpdateAsync(user);
                        string smsText = "به خانواده بزرگ آرمیس خوش آمدید. کد فعالسازی حساب شما : " + token.ToString();
                        string mCodeSender = sms.SendSms(smsText, model.Mobile);
                    }
                    #endregion
                    #region Send TicketSupport
                    TblTicket tbl = new TblTicket();
                    tbl.Title = "درخواست تدریس در آکادمی آرمیس";
                    tbl.Date = DateTime.Now;
                    tbl.UserID = user.Id;
                    tbl.Status = 0;
                    tbl.Section = 6;
                    tbl.Priority = 2;
                    tbl.RecivedID = _userManager.GetUsersInRoleAsync("Admin").Result.FirstOrDefault().Id;
                    _context.Add(tbl);
                    _context.SaveChanges();

                    TblTicketMsg msg = new TblTicketMsg();
                    msg.Date = DateTime.Now;
                    msg.Text = "درخواست تدریس در آکادمی آرمیس";
                    msg.ReceiverID = _userManager.GetUsersInRoleAsync("Admin").Result.FirstOrDefault().Id;
                    msg.SenderID = user.Id;
                    msg.TicketID = tbl.ID;
                    _context.Add(msg);
                    _context.SaveChanges();

                    #region Notices To Admin
                    string AdminEmail = "krm.insert@gmail.com";

                    string EmailTemplate2 = "";
                    string Link = "https://armisacademy.com/Admin/SupportDetails/" + tbl.ID;
                    using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email.html")))
                    {
                        EmailTemplate2 = reader.ReadToEnd();
                    }
                    //string text = "جهت مشاهده جزئیات تیکت بر روی مشاهده کلیک نمایید";
                    string Body2 = EmailTemplate2.Replace("[TITLE]", "تیکت جدید در آرمیس آکادمی").Replace("[TEXT]", msg.Text)
                        .Replace("[SENDER]", model.FirstName + " " + model.LastName)
                        .Replace("[COURSE]", "-")
                        .Replace("[LINK]", HtmlEncoder.Default.Encode(Link)).Replace("[LINK-TITLE]", "مشاهده");
                    await _emailSender.SendEmailNoticAsync(AdminEmail, "تیکت جدید", Body2);
                    #endregion

                    #endregion
                    _logger.LogInformation("User created a new account with password.");

                    return Json(new { result = "ok", msg = "درخواست شما با موفقیت ارسال شد ، و یک حساب کاربری برای شما ایجاد گردید." +
                        " پس از فعالسازی حساب ، از طریق تیکت ها در حساب خود ، وضعیت درخواست را پیگیری نمایید" });
                }
                AddErrors(result);
                // If we got this far, something failed, redisplay form
                return Json(new { result = "faill", msg = "خطا در عملیات لطفا با پشتیبانی تماس بگیرید" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال . لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        [HttpPost]
        public async Task<IActionResult> BecomingTeacher2()
        {
            try
            {
                string UserID = User.GetUserID();
                #region Send TicketSupport
                TblTicket tbl = new TblTicket();
                tbl.Title = "درخواست تدریس در آکادمی آرمیس";
                tbl.Date = DateTime.Now;
                tbl.UserID = UserID;
                tbl.Status = 0;
                tbl.Priority = 2;
                tbl.Section = 6;
                tbl.RecivedID = _userManager.GetUsersInRoleAsync("Admin").Result.FirstOrDefault().Id;
                _context.Add(tbl);
                _context.SaveChanges();

                TblTicketMsg msg = new TblTicketMsg();
                msg.Date = DateTime.Now;
                msg.Text = "درخواست تدریس در آکادمی آرمیس";
                msg.ReceiverID = _userManager.GetUsersInRoleAsync("Admin").Result.FirstOrDefault().Id;
                msg.SenderID = UserID;
                msg.TicketID = tbl.ID;
                _context.Add(msg);
                _context.SaveChanges();

                #region Notices To Admin
                string AdminEmail = "krm.insert@gmail.com";

                string EmailTemplate2 = "";
                string Link = "https://armisacademy.com/Admin/SupportDetails/" + tbl.ID;
                using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email.html")))
                {
                    EmailTemplate2 = reader.ReadToEnd();
                }
                //string text = "جهت مشاهده جزئیات تیکت بر روی مشاهده کلیک نمایید";
                string Body2 = EmailTemplate2.Replace("[TITLE]", "تیکت جدید در آرمیس آکادمی").Replace("[TEXT]", msg.Text)
                    .Replace("[SENDER]", User.GetUserDetails().FirstName + " " + User.GetUserDetails().LastName)
                    .Replace("[COURSE]", "-")
                    .Replace("[LINK]", HtmlEncoder.Default.Encode(Link)).Replace("[LINK-TITLE]", "مشاهده");
                await _emailSender.SendEmailNoticAsync(AdminEmail, "تیکت جدید", Body2);
                #endregion

                #endregion
                return Json(new { result = "ok", msg = "درخواست شما با موفقیت ثبت شد . " +
                    "از طریق تیکت ها در حساب کاربری خود می توانید درخواست خود را پیگیری نمایید" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال . لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        [HttpGet]
        [AllowAnonymous]
        [Route("Reagent/{Id?}")]
        public IActionResult Reagent(string ID)
        {
            var qReagent = _context.Users.Where(a => a.ReagentCode == ID).FirstOrDefault();
            if (qReagent == null)
            {
                return RedirectToAction(nameof(Index));
            }
            TempData["ReagentCode"] = ID;
            return RedirectToAction(nameof(Login));
        }

        public JsonResult Follow(string ID)
        {
            string UserID = User.GetUserID();
            var qFriend = _context.TblFriends.Where(a => a.UserSender == UserID).Where(a => a.UserReceiver == ID).FirstOrDefault();
            if (qFriend == null)
            {
                TblFriends t = new TblFriends();
                t.UserSender = User.GetUserID();
                t.UserReceiver = ID;
                t.Date = DateTime.Now;
                _context.Add(t);
                _context.SaveChanges();

                return Json(new { result = "ok", type = "follow" });
            }
            else
            {
                _context.Remove(qFriend);
                _context.SaveChanges();

                return Json(new { result = "ok", type = "unfollow" });
            }

        }
        [HttpGet]
        [AllowAnonymous]
        [Route("Profile/{userName?}")]
        public async Task<IActionResult> Profile(string UserName)
        {
            if (UserName == null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    UserName = User.GetUserDetails().UserName;
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            var qUser = await _userManager.FindByNameAsync(UserName);
            if (qUser == null)
            {
                return RedirectToAction("Index", "Home");
            }
            string UserID = User.GetUserID();

            FileRepository RepImg = new FileRepository();
            UserRepository RepUser = new UserRepository();
            TimeUtility Time = new TimeUtility();

            VmUser vm = new VmUser();
            vm.ID = qUser.Id;
            vm.FullName = qUser.FirstName + " " + qUser.LastName;
            vm.UserName = qUser.UserName;
            vm.Description = qUser.Description;
            vm.LastEntry = Time.GetTimeName(qUser.LastEntry);
            vm.Score = qUser.Score.ToString();
            vm.Status = qUser.Status;
            string roleName = _userManager.GetRolesAsync(qUser).Result.FirstOrDefault();
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
            var Image = RepImg.GetImageByID(qUser.ImageID);
            vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

            #region UserInfo
            vm.Follow = new UserFollow();
            var qFollow = _context.TblFriends;
            if (UserID != qUser.Id)
            {
                var qFollowed = qFollow.Where(a => a.UserSender == UserID).Where(a => a.UserReceiver == qUser.Id).SingleOrDefault();
                if (qFollowed != null)
                {
                    vm.Follow.IsFollowed = true;
                }
            }
            if (qUser.IsSignedIn && qUser.LastEntry.AddMinutes(30) > DateTime.Now)
            {
                vm.Follow.IsOnline = true;
            }
            vm.Follow.FollowingCount = qFollow.Where(a => a.UserSender == qUser.Id).Count();
            vm.Follow.FollowerCount = qFollow.Where(a => a.UserReceiver == qUser.Id).Count();
            #endregion

            ViewBag.ProfileProgress = RepUser.getProfileProgres(qUser.Id);
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "پروفایل من";
            ViewData["currentNav"] = "profile";
            return View(vm);
        }
        [HttpGet]
        [AllowAnonymous]
        [Route("Profile2/{userName?}")]
        public async Task<IActionResult> Profile2(string UserName)
        {
            if (UserName == null)
            {
                if (User.Identity.IsAuthenticated)
                {
                    UserName = User.GetUserDetails().UserName;
                }
                else
                {
                    return RedirectToAction("Index", "Home");
                }
            }
            var qUser = await _userManager.FindByNameAsync(UserName);
            if (qUser == null)
            {
                return RedirectToAction("Index", "Home");
            }
            string UserID = User.GetUserID();

            FileRepository RepImg = new FileRepository();
            UserRepository RepUser = new UserRepository();
            TimeUtility Time = new TimeUtility();

            VmUser vm = new VmUser();
            vm.ID = qUser.Id;
            vm.FullName = qUser.FirstName + " " + qUser.LastName;
            vm.UserName = qUser.UserName;
            vm.Description = qUser.Description;
            vm.LastEntry = Time.GetTimeName(qUser.LastEntry);
            vm.Score = qUser.Score.ToString();
            vm.Status = qUser.Status;
            string roleName = _userManager.GetRolesAsync(qUser).Result.FirstOrDefault();
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
            var Image = RepImg.GetImageByID(qUser.ImageID);
            vm.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

            #region UserInfo
            vm.Follow = new UserFollow();
            var qFollow = _context.TblFriends;
            if (UserID != qUser.Id)
            {
                var qFollowed = qFollow.Where(a => a.UserSender == UserID).Where(a => a.UserReceiver == qUser.Id).SingleOrDefault();
                if (qFollowed != null)
                {
                    vm.Follow.IsFollowed = true;
                }
            }
            if (qUser.IsSignedIn && qUser.LastEntry.AddMinutes(30) > DateTime.Now)
            {
                vm.Follow.IsOnline = true;
            }
            vm.Follow.FollowingCount = qFollow.Where(a => a.UserSender == qUser.Id).Count();
            vm.Follow.FollowerCount = qFollow.Where(a => a.UserReceiver == qUser.Id).Count();
            #endregion

            ViewBag.ProfileProgress = RepUser.getProfileProgres(qUser.Id);
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "پروفایل من";
            ViewData["currentNav"] = "profile";
            return View(vm);
        }
        [Route("Profile/MyWallet")]
        public IActionResult MyWallet()
        {
            var UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کیف پول من";
            ViewData["currentNav"] = "wallet";
            return View();
        }
        [Route("Profile/MyWallet2")]
        public IActionResult MyWallet2(int Page=1)
        {
            var UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var qPayment = _context.TblTransaction.Where(a => a.ToUserID == UserID || a.TblInvoice.TblCourse.TeacherID == UserID)
                .Include(a => a.TblUser)
                .Include(a => a.TblInvoice).ThenInclude(a => a.TblCourse)
                .OrderByDescending(a => a.Date).AsQueryable();

            int Take = 14;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = qPayment.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            if (qPayment.Count() > 0)
            {
                qPayment = qPayment.Skip(Skip).Take(Take);
            }
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
                vm.NewInventory = item.NewInventory == null ? 0 : (int)item.NewInventory;
                vm.BoonID = 0;
                if (item.TblInvoice != null && item.TblInvoice.BoonID != null)
                {
                    vm.BoonID = (int)item.TblInvoice.BoonID;
                }
                Lstvm.Add(vm);
            }

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کیف پول من";
            ViewData["currentNav"] = "wallet";
            return View(Lstvm);
        }
        public JsonResult GetFinancial()
        {
            string UserID = User.GetUserID();
            var qPayment = _context.TblTransaction.Where(a => a.ToUserID == UserID || a.UserID == UserID)
                .Include(a => a.TblInvoice).Where(a => a.Type == 3 || a.Type == 4)
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
                Lstvm.Add(vm);
            }
            if (User.IsInRole("Teacher"))
            {
                var qEventsInvoice = _context.TblUserEventsInvoice.Where(a => a.TeacherID == UserID).ToList();

                foreach (var item in qEventsInvoice)
                {
                    VmInvoice vm = new VmInvoice();
                    vm.ID = item.ID;
                    vm.FullName = item.TblUserEvents.TblUser.FirstName + " " + item.TblUserEvents.TblUser.LastName;
                    vm.Description = item.Description;
                    vm.Type = 6;
                    vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - HH:mm");
                    vm.Amount = item.Amount;
                    Lstvm.Add(vm);
                }
            }
            return Json(Lstvm.OrderByDescending(a=>a.Date));
        }
        public async Task <JsonResult> EditProfilePic(string ID,string File)
        {
            var user = await _context.Users.Where(a => a.Id == ID).Include(a => a.TblImage).SingleOrDefaultAsync();

            var base64Data = Regex.Match(File, @"data:image/(?<type>.+?),(?<data>.+)").Groups["data"].Value;
            var binData = Convert.FromBase64String(base64Data);

            var stream = new MemoryStream(binData);
            #region ProfileImage
            if (stream != null)
            {
                Random rnd = new Random();
                Ftp MyFtp = new Ftp();
                string FileName = "UserImg-" + user.UserName + rnd.Next(10000) + ".jpg";
                int FtpID = MyFtp.Upload("UserImg", FileName, stream);
                if (FtpID != -1)
                {
                    TblImage tImage = new TblImage();
                    tImage.Alt = "تصویر" + " " + user.FirstName+ " " + user.LastName;
                    tImage.FileName = FileName;
                    tImage.ServerID = FtpID;
                    tImage.Title = "تصویر کاریری";
                    _context.TblImage.Add(tImage);
                    await _context.SaveChangesAsync();
                    if (!user.TblImage.FileName.Contains("Default.jpg"))
                    {
                        int OldImageID = user.ImageID.GetValueOrDefault(-1);
                        user.ImageID = tImage.ID;
                        await _userManager.UpdateAsync(user);

                        if (OldImageID != -1 && user.ImageID != 8 && user.ImageID != null)
                        {
                            var qImage = _context.TblImage.Where(a => a.ID == OldImageID).Include(a => a.TblServer).Single();
                            if (qImage != null)
                            {
                                MyFtp.Remove(qImage.TblServer.ID, qImage.FileName);
                                _context.Remove(qImage);
                                await _context.SaveChangesAsync();
                            }
                        }
                    }
                    else
                    {
                        user.ImageID = tImage.ID;
                        await _userManager.UpdateAsync(user);
                    }
                }
                return Json(new { result = "ok", msg = "تغییرات با موفقیت انجام گردید" });

            }
            #endregion ProfileImage
            return Json(new { result = "faill", msg = "خطا در انجام عملیات" });
        }
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"(\+98|0)?9\d{9}").Success;
        }
        public bool IsValidEmail(string emailaddress)
        {
            try
            {
                MailAddress m = new MailAddress(emailaddress);
                return true;
            }
            catch (FormatException)
            {
                return false;
            }
        }
        [Route("Profile/Settings")]
        public async Task<IActionResult> Settings()
        {
            try
            {
                string UserID = User.GetUserID();
                var user = await _userManager.FindByIdAsync(UserID);
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                VmUser vm = new VmUser();
                FileRepository RepImg = new FileRepository();
                UserRepository RepUser = new UserRepository();
                vm.ID = user.Id;
                //vm.UserName = user.UserName;
                vm.FullName = user.FirstName + " " + user.LastName;
                vm.FirstName = user.FirstName;
                vm.Status = user.Status;
                vm.LastName = user.LastName;
                vm.Mobile = user.Mobile;
                vm.PostalCode = user.PostalCode;
                vm.CodeMelli = user.CodeMelli;
                //vm.ShansnamehNumber = user.ShansnamehNumber;
                vm.Description = user.Description;
                vm.City = user.City;
                vm.State = user.State;
                vm.PhoneNumber = user.PhoneNumber;
                vm.ReagentCode = user.ReagentCode;
                vm.Email = user.Email != null ? user.Email : "";
                vm.Address = user.Address;
                vm.Social = new UserSoical();

                var qUserSocial = _context.TblUserSocial.Where(a => a.UserID == user.Id).FirstOrDefault();
                if (qUserSocial != null)
                {
                    vm.Social.TelegramID = qUserSocial.TelegramID;
                    vm.Social.SkypeID = qUserSocial.SkypeID;
                    vm.Social.LinkedinID = qUserSocial.LinkedinID;
                    vm.Social.InstagramID = qUserSocial.InstagramID;
                }
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

                var qScore = _context.TblScore.Where(a => a.TitleEn == "FillProfile").SingleOrDefault();
                var qUserScore = _context.TblUserScore.Where(a => a.ID == qScore.ID && a.UserID == UserID).FirstOrDefault();
                ViewBag.ProfileProgress = RepUser.getProfileProgres(user.Id);
                if (qUserScore == null && ViewBag.ProfileProgress == 100)
                {
                    TblUserScore t = new TblUserScore();
                    t.UserID = user.Id;
                    t.ScoreID = qScore.ID;
                    _context.Add(t);
                    _context.SaveChanges();

                    user.Score += qScore.Value;
                    await _userManager.UpdateAsync(user);
                }

                #region UserInfo
                TimeUtility Time = new TimeUtility();
                vm.Score = user.Score.ToString();
                vm.LastEntry = Time.GetTimeName(user.LastEntry);
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

                ViewData["role"] = _userManager.GetRolesAsync(user).Result.FirstOrDefault();
                ViewData["currentTab"] = "home";
                ViewData["mainBreadcrumb"] = "پروفایل";
                ViewData["breadcrumb"] = "تنظیمات پروفایل";
                ViewData["currentNav"] = "settings";
                return View(vm);
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Home");
            }
        }
        [Route("Profile/Settings2")]
        public async Task<IActionResult> Settings2()
        {
            try
            {
                string UserID = User.GetUserID();
                var user = await _userManager.FindByIdAsync(UserID);
                if (user == null)
                {
                    return RedirectToAction("Index", "Home");
                }
                VmUser vm = new VmUser();
                FileRepository RepImg = new FileRepository();
                UserRepository RepUser = new UserRepository();
                vm.ID = user.Id;
                //vm.UserName = user.UserName;
                vm.FullName = user.FirstName + " " + user.LastName;
                vm.FirstName = user.FirstName;
                vm.Status = user.Status;
                vm.LastName = user.LastName;
                vm.Mobile = user.Mobile;
                vm.PostalCode = user.PostalCode;
                vm.CodeMelli = user.CodeMelli;
                //vm.ShansnamehNumber = user.ShansnamehNumber;
                vm.Description = user.Description;
                vm.City = user.City;
                vm.State = user.State;
                vm.PhoneNumber = user.PhoneNumber;
                vm.ReagentCode = user.ReagentCode;
                vm.Email = user.Email != null ? user.Email : "";
                vm.Address = user.Address;
                vm.Social = new UserSoical();

                var qUserSocial = _context.TblUserSocial.Where(a => a.UserID == user.Id).FirstOrDefault();
                if (qUserSocial != null)
                {
                    vm.Social.TelegramID = qUserSocial.TelegramID;
                    vm.Social.SkypeID = qUserSocial.SkypeID;
                    vm.Social.LinkedinID = qUserSocial.LinkedinID;
                    vm.Social.InstagramID = qUserSocial.InstagramID;
                }
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

                var qScore = _context.TblScore.Where(a => a.TitleEn == "FillProfile").SingleOrDefault();
                var qUserScore = _context.TblUserScore.Where(a => a.ID == qScore.ID && a.UserID == UserID).FirstOrDefault();
                ViewBag.ProfileProgress = RepUser.getProfileProgres(user.Id);
                if (qUserScore == null && ViewBag.ProfileProgress == 100)
                {
                    TblUserScore t = new TblUserScore();
                    t.UserID = user.Id;
                    t.ScoreID = qScore.ID;
                    _context.Add(t);
                    _context.SaveChanges();

                    user.Score += qScore.Value;
                    await _userManager.UpdateAsync(user);
                }

                #region UserInfo
                TimeUtility Time = new TimeUtility();
                vm.Score = user.Score.ToString();
                vm.LastEntry = Time.GetTimeName(user.LastEntry);
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

                ViewData["role"] = _userManager.GetRolesAsync(user).Result.FirstOrDefault();
                ViewData["currentTab"] = "home";
                ViewData["mainBreadcrumb"] = "پروفایل";
                ViewData["breadcrumb"] = "تنظیمات پروفایل";
                ViewData["currentNav"] = "settings";
                return View(vm);
            }
            catch (Exception)
            {
                return RedirectToAction("Index", "Home");
            }
        }
        [Route("Profile/Terms")]
        public async Task<IActionResult> Terms()
        {
            var UserID = User.GetUserID();
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            var user = await _userManager.FindByIdAsync(User.GetUserID());

            VmUser vm = new VmUser();
            
            var qGuide = _context.TblGuide.OrderByDescending(a => a.ID).AsQueryable();
            if(roleName== "Student")
            {
                qGuide = qGuide.Where(a => a.Type == 0 || a.Type == 2);
            }
            if (roleName == "Teacher")
            {
                qGuide = qGuide.Where(a => a.Type == 1 || a.Type == 3);
            }
            vm.LstTerms = new List<UserrTerms>();
            foreach (var item in qGuide.ToList())
            {
                UserrTerms ut = new UserrTerms();
                ut.ID = item.ID;
                ut.Text = item.Text;
                ut.Title = item.Title;
                ut.Type = item.Type;
                vm.LstTerms.Add(ut);
            }
            ViewData["currentNav"] = "terms";
            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "قوانین و مقررات";

            return View(vm);
        }

        [Route("Profile/Earning")]
        public IActionResult Earning()
        {
            var UserID = User.GetUserID();

            var qEarn = _context.TblUserEarn.Where(a => a.UserID == UserID)
                .Include(a=>a.TblUser).Include(a=>a.TblUserEarnLinks)
                .Include(a=>a.TblEarnReagent).SingleOrDefault();
            VmEarning vm = new VmEarning();

            if (qEarn != null)
            {
                ViewBag.Submited = qEarn.Status;
                if (qEarn.Status == 1)
                {
                    vm.ID = qEarn.ID;
                    vm.TotalVisit = qEarn.TblUserEarnLinks.Sum(a => a.Visit);
                    vm.TotalSales = qEarn.TotalSales;
                    vm.TotalEarn= qEarn.TblEarnReagent.Sum(a=>a.Amount) + " تومان";
                    vm.Inventory = qEarn.Inventory +" تومان";

                    #region UserStep
                    int TotalSale = qEarn.TotalSales;
                    //var qUserStep = _context.TblUserStepEarn.Where(a => a.UserID == UserID).FirstOrDefault();
                    //vm.UserStepID = qUserStep != null ? qUserStep.ID : 1;
                    //if (qUserStep == null)
                    //{
                    //    TblUserStepEarn tu = new TblUserStepEarn();
                    //    tu.StepID = 1;
                    //    tu.UserID = UserID;
                    //    _context.Add(tu);
                    //    _context.SaveChanges();
                    //}
                    //if (qEarn.TotalSales <= 10)
                    //{
                    //    qUserStep.ID = 1;
                    //}
                    #endregion
                    vm.UserCode = qEarn.UserCode;

                    vm.ListStepEarn = new List<VmStepEarn>();
                    var qStep = _context.TblStepEarn.ToList();
                    for (int i = 0; i < qStep.Count; i++)
                    {
                        var item = qStep[i];
                        VmStepEarn vs = new VmStepEarn();
                        vs.ID = item.ID;
                        vs.Value = item.Value;
                        vs.Rate = item.Rate;
                        if (TotalSale >= 10)
                        {
                            if (TotalSale >= item.Value)
                            {
                                vs.Checked = true;
                            }
                            else if (TotalSale >= qStep[i - 1].Value && TotalSale <= item.Value)
                            {
                                vs.Checked = true;
                            }
                        }
                        vm.ListStepEarn.Add(vs);
                    }
                    //vm.ListEarnLinks = new List<VmEarnLinks>();
                    //var qEarnLinks = _context.TblUserEarnLinks.Where(a => a.UserEarnID == vm.ID).ToList();
                    //foreach (var item in qEarnLinks)
                    //{
                    //    VmEarnLinks ve = new VmEarnLinks();
                    //    ve.Title = item.Title;
                    //    ve.Link = item.Link;
                    //    ve.Visit = item.Visit;
                    //    ve.BuyCount = item.BuyCount;
                    //    vm.ListEarnLinks.Add(ve);
                    //}
                    var qEarnTransaction = _context.TblEarnTransaction.Where(a => a.UserID == UserID).ToList();
                    vm.ListEarnTransaction = new List<VmEarnTransaction>();
                    foreach (var item in qEarnTransaction)
                    {
                        VmEarnTransaction ve = new VmEarnTransaction();
                        ve.ID = item.ID;
                        //ve.Amount = item.Amount + " تومان";
                        //ve.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd HH:mm");
                        //ve.TrackingCode = item.TrackingCode;
                        //ve.Description = item.Description;
                        ve.Status = item.Status;
                        vm.ListEarnTransaction.Add(ve);
                    }
                    ViewBag.BankList = _context.TblBank.Where(a => a.UserID == UserID).ToList();
                }
            }
            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "کسب درآمد";
            ViewData["currentNav"] = "earning";
            return View(vm);
        }
        public JsonResult GetEarnLinks()
        {
            string UserID = User.GetUserID();
            var qEarn = _context.TblUserEarn.Where(a => a.UserID == UserID)
                .Include(a => a.TblUser).Include(a => a.TblUserEarnLinks).SingleOrDefault();

            List<VmEarnLinks> Lstvm = new List<VmEarnLinks>();
            var qEarnLinks = _context.TblUserEarnLinks.Where(a => a.UserEarnID == qEarn.ID).ToList();
            foreach (var item in qEarnLinks)
            {
                VmEarnLinks ve = new VmEarnLinks();
                ve.Title = item.Title;
                ve.Link = item.Link;
                ve.Visit = item.Visit;
                ve.BuyCount = item.BuyCount;
                Lstvm.Add(ve);
            }
            return Json(Lstvm);
        }
        public JsonResult GetEarnTransaction()
        {
            string UserID = User.GetUserID();
            var qEarnTransaction = _context.TblEarnTransaction.Where(a => a.UserID == UserID).ToList();

            List<VmEarnTransaction> Lstvm = new List<VmEarnTransaction>();
            foreach (var item in qEarnTransaction)
            {
                VmEarnTransaction ve = new VmEarnTransaction();
                ve.ID = item.ID;
                ve.Amount = item.Amount + " تومان";
                ve.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd HH:mm");
                ve.TrackingCode = item.TrackingCode;
                ve.Description = item.Description;
                ve.Status = item.Status;

                Lstvm.Add(ve);
            }
            return Json(Lstvm);
        }
        public JsonResult GetEarnStatis(int ID)
        {
            string UserID = User.GetUserID();
            var qEarnTransaction = _context.TblEarnReagent.Where(a => a.UserEarnID == ID)
                .Include(a=>a.TblInvoice).ThenInclude(a=>a.TblCourse).ToList();

            List<VmEarnReagent> Lstvm = new List<VmEarnReagent>();
            foreach (var item in qEarnTransaction)
            {
                VmEarnReagent ve = new VmEarnReagent();
                ve.ID = item.ID;
                ve.Amount = item.Amount;
                ve.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd HH:mm");
                ve.CourseName = item.TblInvoice.TblCourse.Title;
                ve.Type = item.Type;

                Lstvm.Add(ve);
            }
            return Json(Lstvm);
        }

        public IActionResult AddLink(TblUserEarnLinks t)
        {
            try
            {
                if (!ValidateUrl(t.Link))
                {
                    TempData["Message"] = "لینک وارد شده معتبر نمی باشد";
                    TempData["Style"] = "alert-danger";
                }
                _context.Add(t);
                _context.SaveChanges();
                string MyLink = "https://" + Request.Host + "/Elink/" + t.ID;
                t.Link = MyLink;
                _context.Update(t);
                _context.SaveChanges();

                TempData["Message"] = "لینک اختصاصی با موفقیت ثبت شد";
                TempData["Style"] = "alert-success";
            }
            catch (Exception)
            {
                TempData["Message"] = "خطا در ثبت . اطلاعات وروودی نامعتبر می باشد";
                TempData["Style"] = "alert-danger";
            }
            return RedirectToAction(nameof(Earning));
        }
        public async Task<IActionResult> DepositRequest(TblEarnTransaction t)
        {
            string UserID= User.GetUserID();
            var qUser = await _context.TblUserEarn.Where(a => a.UserID == UserID).SingleOrDefaultAsync();
            if (qUser.Inventory < t.Amount)
            {
                TempData["Message"] = "مقدار درخواستی شما از مقدار موجودی شما بیشتر می باشد";
                TempData["Style"] = "alert-danger";
                return RedirectToAction(nameof(Earning));
            }
            t.UserID = UserID;
            t.Date = DateTime.Now;
            t.Status = 0;
            _context.Add(t);
            _context.SaveChanges();

            TempData["Message"] = "درخواست شما با موفقیت ارسال شد و در حال بررسی  قرار گرفت";
            TempData["Style"] = "alert-success";
            return RedirectToAction(nameof(Earning));

        }
        public JsonResult CreateELink(string Link="")
        {
            try
            {
                if (!ValidateUrl(Link))
                {
                    return Json(new { result = "faill", msg = "لینک وارد شده معتبر نمی باشد" });
                }
                //TblUserEarnLinks t = new TblUserEarnLinks();
                string MyLink = "https://" + Request.Host + "/Elink/524";
                return Json(new { result = "ok", link = MyLink, msg = "لینک اختصاصی شما ایجاد شد" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال" });
            }
        }
        [HttpPost]
        public IActionResult SubmitEarning()
        {
            var UserID = User.GetUserID();

            TblUserEarn t = new TblUserEarn();
            t.Status = 0;
            t.UserID = UserID;

            _context.Add(t);
            _context.SaveChanges();

            TempData["Message"] = "درخواست شما با موفقیت ارسال شد و بزودی بررسی می شود";
            TempData["Style"] = "alert-light-success";
            return RedirectToAction(nameof(Earning));
        }

        [HttpGet]
        [Route("Profile/BankDetails")]
        public IActionResult BankDetails()
        {
            string UserID = User.GetUserID();
            var qBank = _context.TblBank
                .Where(a => a.UserID == UserID)
                .OrderByDescending(a => a.ID).Include(a => a.TblUser).ToList();
            ViewBag.UserID = UserID;

            UserRepository Rep_User = new UserRepository();
            string roleName = _userManager.GetRolesAsync(User.GetUserDetails()).Result.FirstOrDefault();
            ViewBag.ProfileDetails = Rep_User.GetProfileDetails(UserID, roleName);

            ViewData["mainBreadcrumb"] = "پروفایل";
            ViewData["breadcrumb"] = "اطلاعات بانکی";
            ViewData["currentNav"] = "wallet";
            return View(qBank);
        }
        [HttpPost]
        public IActionResult BankDetails(TblBank t)
        {
            try
            {
                _context.Add(t);
                _context.SaveChanges();

                TempData["Style"] = "success";
                TempData["Message"] = "حساب جدید با موفقیت اضافه گردید";

                return Redirect("/Profile/BankDetails");
            }
            catch (Exception)
            {
                TempData["Style"] = "danger";
                TempData["Message"] = "داده های ورودی نامعتبر می باشد";
                return Redirect("/Profile/BankDetails");
            }
        }
        [HttpPost]
        public IActionResult EditBankDetails(int ID, string UserID, string BankName, string CardNumber, string ShabaNumber, string AccountNumber)
        {
            try
            {
                if (string.IsNullOrEmpty(BankName) || string.IsNullOrEmpty(CardNumber) || string.IsNullOrEmpty(ShabaNumber) || string.IsNullOrEmpty(AccountNumber))
                {
                    TempData["Style"] = "alert-danger";
                    TempData["Message"] = "پر کردن تمامی فیلد ها ضروری می باشد";
                    return RedirectToAction(nameof(BankDetails), new { ID = UserID });
                }
                var qBank = _context.TblBank.Where(a => a.ID == ID).SingleOrDefault();
                qBank.AccountNumber = AccountNumber;
                qBank.BankName = BankName;
                qBank.CardNumber = CardNumber;
                qBank.ShabaNumber = ShabaNumber;

                _context.Update(qBank);
                _context.SaveChanges();

                TempData["Style"] = "alert-success";
                TempData["Message"] = "حساب مورد نظر با موفقیت ویرایش گردید";
            }
            catch (Exception)
            {
                TempData["Style"] = "alert-danger";
                TempData["Message"] = "خطا در ثبت اطلاعات";
            }
            return RedirectToAction(nameof(BankDetails), new { ID = UserID });
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
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> NotConfirm(string user)
        {
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }
            //ViewBag.UserID = user;
            var qUser = await _userManager.FindByIdAsync(user);
            return View(qUser);
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> NotConfirm(string userId,string code="")
        {
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }
            var qUser = await _userManager.FindByIdAsync(userId);
            if (qUser != null)
            {
                string[] Value = qUser.MobileConfirmCode.Split(new string[] { "," }, StringSplitOptions.None);
                if (Value[0] != "" && Value[0] == code)
                {
                    qUser.Status = 1;
                    qUser.MobileConfirmCode = "";
                    //user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(qUser);
                    await _signInManager.SignInAsync(qUser, isPersistent: false);
                    return RedirectToAction(nameof(ConfirmedAccount));
                }
                TempData["Message"] = "کد فعالسازی نامعتبر می باشد";
                return RedirectToAction(nameof(NotConfirm), new { user = qUser.Id });
            }
            TempData["Message"] = "حساب کاربری نامعتبر می باشد";
            return RedirectToAction(nameof(NotConfirm), new { user = qUser.Id });
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> VerifyAccount(string userId, string code = "")
        {
            try
            {
                if (userId == null)
                {
                    return Json(new { result = "faill", msg = "حساب کاربری نامعتبر می باشد" });
                }
                var qUser = await _userManager.FindByIdAsync(userId);
                if (qUser != null)
                {
                    string[] Value = qUser.MobileConfirmCode.Split(new string[] { "," }, StringSplitOptions.None);
                    if (Value[0] != "" && Value[0] == code)
                    {
                        qUser.Status = 1;
                        qUser.MobileConfirmCode = "";
                        //user.EmailConfirmed = true;
                        await _userManager.UpdateAsync(qUser);
                        await _signInManager.SignInAsync(qUser, isPersistent: false);
                        return Json(new { result = "ok", msg = "حساب کاربری با موفقیت فعال شد" });
                    }
                    return Json(new { result = "faill", msg = "کد فعالسازی نامعتبر می باشد" });
                }
                return Json(new { result = "faill", msg = "حساب کاربری نامعتبر می باشد" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطای امنیتی لطفا در زمان دیگری تلاش نمایید" });
            }

        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWith2fa(bool rememberMe, string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();

            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var model = new LoginWith2faViewModel { RememberMe = rememberMe };
            ViewData["ReturnUrl"] = returnUrl;

            return View(model);
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWith2fa(LoginWith2faViewModel model, bool rememberMe, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var authenticatorCode = model.TwoFactorCode.Replace(" ", string.Empty).Replace("-", string.Empty);

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(authenticatorCode, rememberMe, model.RememberMachine);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with 2fa.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            else if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid authenticator code entered for user with ID {UserId}.", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid authenticator code.");
                return View();
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> LoginWithRecoveryCode(string returnUrl = null)
        {
            // Ensure the user has gone through the username & password screen first
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            ViewData["ReturnUrl"] = returnUrl;

            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LoginWithRecoveryCode(LoginWithRecoveryCodeViewModel model, string returnUrl = null)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                throw new ApplicationException($"Unable to load two-factor authentication user.");
            }

            var recoveryCode = model.RecoveryCode.Replace(" ", string.Empty);

            var result = await _signInManager.TwoFactorRecoveryCodeSignInAsync(recoveryCode);

            if (result.Succeeded)
            {
                _logger.LogInformation("User with ID {UserId} logged in with a recovery code.", user.Id);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                _logger.LogWarning("User with ID {UserId} account locked out.", user.Id);
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                _logger.LogWarning("Invalid recovery code entered for user with ID {UserId}", user.Id);
                ModelState.AddModelError(string.Empty, "Invalid recovery code entered.");
                return View();
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Lockout()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SuccessReg(string user)
        {
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }
            var qUser = await _userManager.FindByIdAsync(user);

            if (qUser != null)
            {
                return View(qUser);
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> SuccessReg(string userId, string code)
        {
            if (code == null)
            {
                TempData["Message"] = "لطفا کد فعالسازی را وارد نمایید";
                return RedirectToAction(nameof(NotConfirm), new { user = userId });
            }
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }
            var qUser = await _userManager.FindByIdAsync(userId);
            if (qUser != null)
            {
                string[] Value = qUser.MobileConfirmCode.Split(new string[] { "," }, StringSplitOptions.None);
                if (Value[0] != "" && Value[0] == code)
                {
                    qUser.Status = 1;
                    qUser.MobileConfirmCode = "";
                    //user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(qUser);
                    return RedirectToAction(nameof(ConfirmedAccount));
                }
                TempData["Message"] = "کد فعالسازی نامعتبر می باشد";
                return RedirectToAction(nameof(SuccessReg), new { user = qUser.Id });
            }
            TempData["Message"] = "حساب کاربری نامعتبر می باشد";
            return RedirectToAction(nameof(SuccessReg), new { user = qUser.Id });
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> verification(string user)
        {
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }
            var qUser = await _userManager.FindByIdAsync(user);

            if (qUser != null)
            {
                return View(qUser);
            }
            else
            {
                return RedirectToAction(nameof(Login));
            }
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> verification(string userId, string code)
        {
            if (code == null)
            {
                TempData["Message"] = "لطفا کد فعالسازی را وارد نمایید";
                return RedirectToAction(nameof(NotConfirm), new { user = userId });
            }
            if (userId == null)
            {
                return RedirectToAction(nameof(Login));
            }
            var qUser = await _userManager.FindByIdAsync(userId);
            if (qUser != null)
            {
                string[] Value = qUser.MobileConfirmCode.Split(new string[] { "," }, StringSplitOptions.None);
                if (Value[0] != "" && Value[0] == code)
                {
                    qUser.Status = 1;
                    qUser.MobileConfirmCode = "";
                    //user.EmailConfirmed = true;
                    await _userManager.UpdateAsync(qUser);
                    return RedirectToAction(nameof(ConfirmedAccount));
                }
                TempData["Message"] = "کد فعالسازی نامعتبر می باشد";
                return RedirectToAction(nameof(SuccessReg), new { user = qUser.Id });
            }
            TempData["Message"] = "حساب کاربری نامعتبر می باشد";
            return RedirectToAction(nameof(SuccessReg), new { user = qUser.Id });
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SendEmailConfirm(string userId)
        {
            var qUser = await _userManager.FindByIdAsync(userId);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(qUser);
            var callbackUrl = Url.EmailConfirmationLink(qUser.Id, code, Request.Scheme);

            string EmailTemplate = "";
            using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
            {
                EmailTemplate = reader.ReadToEnd();
            }
            string text = "جهت فعالسازی حساب کاربری خود لینک زیر را کلیک نمایید";
            string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                .Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl)).Replace("[LINK-TITLE]", "تایید حساب");
            await _emailSender.SendEmailConfirmationAsync(qUser.Email, callbackUrl, Body);

            TempData["Message"] = "ایمیل فعالسازی با موفقیت ارسال گردید";
            TempData["Style"] = "alert-success";
            return RedirectToAction(nameof(NotConfirm), new { user = qUser.Id });
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> SendEmailConfirm2(string userId)
        {
            var qUser = await _userManager.FindByIdAsync(userId);

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(qUser);
            var callbackUrl = Url.EmailConfirmationLink(qUser.Id, code, Request.Scheme);

            string EmailTemplate = "";
            using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
            {
                EmailTemplate = reader.ReadToEnd();
            }
            string text = "جهت فعالسازی حساب کاربری خود لینک زیر را کلیک نمایید";
            string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                .Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl)).Replace("[LINK-TITLE]", "تایید حساب");
            await _emailSender.SendEmailConfirmationAsync(qUser.Email, callbackUrl, Body);

            TempData["Message"] = "ایمیل فعالسازی با موفقیت ارسال گردید";
            TempData["Style"] = "alert-success";
            return RedirectToAction(nameof(verification), new { user = qUser.Id });
        }
        [AllowAnonymous]
        [HttpPost]
        public async Task<JsonResult> SendEmailLink(string userId)
        {
            var qUser = await _userManager.FindByIdAsync(userId);
            if (qUser != null)
            {
                var code = await _userManager.GenerateEmailConfirmationTokenAsync(qUser);
                var callbackUrl = Url.EmailConfirmationLink(qUser.Id, code, Request.Scheme);

                string EmailTemplate = "";
                using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                {
                    EmailTemplate = reader.ReadToEnd();
                }
                string text = "جهت فعالسازی حساب کاربری خود لینک زیر را کلیک نمایید";
                string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                    .Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl)).Replace("[LINK-TITLE]", "تایید حساب");
                await _emailSender.SendEmailConfirmationAsync(qUser.Email, callbackUrl, Body);

                TempData["Message"] = "ایمیل فعالسازی با موفقیت ارسال گردید";
                TempData["Style"] = "alert-success";
                return Json(new { result = "ok", msg = "ایمیل فعالسازی با موفقیت ارسال شد" });
            }
            return Json(new { result = "faill", msg = "کاربر نامعتبر می باشد" });

        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            var user = await _userManager.FindByIdAsync(User.GetUserID());
            user.IsSignedIn = false;
            await _userManager.UpdateAsync(user);

            await _signInManager.SignOutAsync();
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }

        [HttpPost]
        [AllowAnonymous]
        //[ValidateAntiForgeryToken]
        public IActionResult ExternalLogin(string provider, string returnUrl = null)
        {
            // Request a redirect to the external login provider.
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string returnUrl = null, string remoteError = null)
        {
            if (remoteError != null)
            {
                ErrorMessage = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login.
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Name} provider.", info.LoginProvider);
                return RedirectToLocal(returnUrl);
            }
            if (result.IsLockedOut)
            {
                return RedirectToAction(nameof(Lockout));
            }
            else
            {
                // If the user does not have an account, then ask the user to create an account.
                ViewData["ReturnUrl"] = returnUrl;
                ViewData["LoginProvider"] = info.LoginProvider;
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                return View("ExternalLogin", new ExternalLoginViewModel { Email = email });
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ExternalLoginConfirmation(ExternalLoginViewModel model, string returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Get the information about the user from the external login provider
                var info = await _signInManager.GetExternalLoginInfoAsync();
                if (info == null)
                {
                    throw new ApplicationException("Error loading external login information during confirmation.");
                }
                var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
                var result = await _userManager.CreateAsync(user);
                if (result.Succeeded)
                {
                    result = await _userManager.AddLoginAsync(user, info);
                    if (result.Succeeded)
                    {
                        await _signInManager.SignInAsync(user, isPersistent: false);
                        _logger.LogInformation("User created an account using {Name} provider.", info.LoginProvider);
                        return RedirectToLocal(returnUrl);
                    }
                }
                AddErrors(result);
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(nameof(ExternalLogin), model);
        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> SendSMSCode(string UserID)
        {
            try
            {
                Random Rnd = new Random();
                int token = Rnd.Next(1000, 9999);
                var User = await _userManager.FindByIdAsync(UserID);

                string mobile = "";
                if (User != null)
                {
                    if (User.Status == 1)
                    {
                        return Json(new { result = "faill", msg = "حساب کاربری شما هم اکنون فعال می باشد" });
                    }
                    string[] Value = User.MobileConfirmCode.Split(new string[] { "," }, StringSplitOptions.None);
                    if (Value[0] == "")
                    {
                        mobile = User.Mobile;
                    }
                    else
                    {
                        mobile = Value[1];
                    }
                    SmsSender sms = new SmsSender();
                    string smsText = "کد فعالسازی حساب شما در آکادمی موسیقی آرمیس : " + token.ToString();
                    string result = sms.SendSms(smsText, mobile);

                    User.MobileConfirmCode = token.ToString() + "," + mobile;
                    _context.Update(User);
                    _context.SaveChanges();

                    return Json(new { result = "ok", msg = "کد فعالسازی با موفقیت ارسال گردید" });
                }
                else
                {
                    return Json(new { result = "faill", msg = "حساب کاربری نامعتبر می باشد" });
                }
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطای امنیتی . لطفا در زمان دیگری تلاش نمایید" });
            }

        }
        [HttpPost]
        [AllowAnonymous]
        public async Task<JsonResult> SendEmailCode(string UserID)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(UserID);
                if (User != null)
                {
                    var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);

                    string EmailTemplate = "";
                    using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                    {
                        EmailTemplate = reader.ReadToEnd();
                    }
                    string text = "لطفا جهت فعالسازی حساب کاربری خود بر روی لینک زیر کلیک نمایید";
                    string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                        .Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl)).Replace("[LINK-TITLE]", "تایید حساب");
                    await _emailSender.SendEmailConfirmationAsync(user.Email, callbackUrl, Body);

                    return Json(new { result = "ok", msg = "لینک فعالسازی با موققیت ارسال گردید" });
                }
                else
                {
                    return Json(new { result = "faill", msg = "حساب کاربری نامعتبر می باشد" });
                }
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در ارسال" });
            }
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ConfirmedAccount()
        {
            return View();
        }
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmedEmail(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
            var user = await _userManager.FindByIdAsync(userId);
            user.Status = 1;
            user.EmailConfirmed = true;
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{userId}'.");
            }
            await _userManager.UpdateAsync(user);
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                await _signInManager.SignInAsync(user, isPersistent: false);
                return View(); 
            }
            TempData["Message"] = "ایمیل فعالسازی منقضی شده است";
            TempData["Style"] = "alert-danger";
            return RedirectToAction(nameof(verification), new { user = user.Id });
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    if (IsPhoneNumber(model.UserName))
                    {
                        var user = _context.Users.Where(a => a.Mobile == model.UserName).FirstOrDefault();
                        if (user == null)
                        {
                            return Json(new { result = "faill", msg = "تلفن همراه وارد شده در سیستم موجود نمی باشد" });
                        }
                        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                        Random rnd = new Random();
                        int NewPassword = rnd.Next(111111, 999999);

                        await _userManager.ResetPasswordAsync(user, code, NewPassword.ToString());

                        SmsSender sms = new SmsSender();
                        string smsText = "کلمه عبور جدید شما جهت ورود به حساب کاربری : " + NewPassword.ToString() + Environment.NewLine + "https://Armisacademy.com";
                        string mCodeSender = sms.SendSms(smsText, model.UserName);

                        return Json(new { result = "ok", msg = "کلمه عبور جدید به  شماره همراه شما ارسال گردید" });
                    }
                    else if (IsValidEmail(model.UserName))
                    {
                        var user = await _userManager.FindByEmailAsync(model.UserName);
                        if (user == null)
                        {
                            // Don't reveal that the user does not exist or is not confirmed
                            return Json(new { result = "faill", msg = "ایمیل وارد شده سیستم موجود نمی باشد" });
                        }

                        var code = await _userManager.GeneratePasswordResetTokenAsync(user);
                        var callbackUrl = Url.ResetPasswordCallbackLink(user.Id, code, Request.Scheme);

                        string EmailTemplate = "";
                        using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                        {
                            EmailTemplate = reader.ReadToEnd();
                        }
                        string text = "جهت تغییر کلمه عبور لینک زیر را کلیک نمایید";
                        string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text)
                            .Replace("[LINK]", callbackUrl).Replace("[LINK-TITLE]", "تغییر کلمه عبور");

                        await _emailSender.SendEmailAsync(model.UserName, "تغییر کلمه عبور", Body);
                        return Json(new { result = "ok", msg = "لینک بازآوری کلمه عبور به ایمیل شما ارسال گردید لطفا ایمیل خود را بررسی نمایید" });
                    }
                    else
                    {
                        return Json(new { result = "faill", msg = "تلفن همراه و یا ایمیل وارد شده نامعتبر می باشد" });
                    }
                }
                return Json(new { result = "faill", msg = "اطلاعات وارد شده نامعتبر می باشد" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "متاسفانه خطایی رخ داد. لطفا در زمان دیگری تلاش نمایید" });
            }


            // If we got this far, something failed, redisplay form
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPasswordConfirmation()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string code = null)
        {
            //if (code == null)
            //{
            //    throw new ApplicationException("A code must be supplied for password reset.");
            //}
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword3(string code = null)
        {
            //if (code == null)
            //{
            //    throw new ApplicationException("A code must be supplied for password reset.");
            //}
            var model = new ResetPasswordViewModel { Code = code };
            return View(model);
        }
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                // Don't reveal that the user does not exist
                ViewBag.Message = "ایمیل وارد شده در سیستم ثبت نشده است";
                ViewBag.Style = "alert-danger";
                return View();
            }
            if (string.IsNullOrEmpty(model.Code))
            {
                ViewBag.Message = "کد امنیتی نامعتبر است";
                ViewBag.Style = "alert-danger";
                return View(model);
            }
            if (model.Password.Length < 6)
            {
                ViewBag.Message = "کلمه عبور حداقل باید 6 کاراکتر باشد";
                ViewBag.Style = "alert-danger";
                return View(model);
            }
            var result = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
            if (result.Succeeded)
            {
                ViewBag.Message = "کلمه عبور شما با موفقیت بروزرسانی گردید. هم اکنون می توانید وارد حساب کاربری خود شوید";
                ViewBag.Style = "alert-success";
                return View();
            }
            else
            {
                ViewBag.Message = "لینک بازیابی کلمه عبور منقضی شده است";
                ViewBag.Style = "alert-danger";
                AddErrors(result);
                return View(model);
            }
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPasswordConfirmation()
        {
            return View();
        }
        public async Task<JsonResult> UpdateUserHistory(string ID)
        {
            var user =await  _userManager.FindByIdAsync(ID);
            user.LastEntry = DateTime.Now;
            await _userManager.UpdateAsync(user);
            return Json("");
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        /// <summary>
        /// Validates a URL.
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        private bool ValidateUrl(string url)
        {
            //Uri validatedUri;

            if (Uri.TryCreate(url, UriKind.Absolute, out Uri validatedUri)) //.NET URI validation.
            {
                //If true: validatedUri contains a valid Uri. Check for the scheme in addition.
                return (validatedUri.Scheme == Uri.UriSchemeHttp || validatedUri.Scheme == Uri.UriSchemeHttps);
            }
            return false;
        }
        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private IActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
            {
                return Redirect(returnUrl);
            }
            else
            {
                return RedirectToAction(nameof(HomeController.Index), "Home");
            }
        }


        #endregion
    }
}
