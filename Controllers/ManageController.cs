using System;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ArmisApp.Models.ManageViewModels;
using ArmisApp.Services;
using ArmisApp.Models.Identity;
using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using Microsoft.EntityFrameworkCore;
using ArmisApp.Models.Repository;
using ArmisApp.Models.ViewModels;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.AspNetCore.Http;

namespace ArmisApp.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ManageController : Controller
    {
        private readonly IHostEnvironment _appEnvironment;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly DataContext _context;
        private readonly IEmailSender _emailSender;
        private readonly ILogger _logger;
        private readonly UrlEncoder _urlEncoder;

        private const string AuthenticatorUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";
        private const string RecoveryCodesKey = nameof(RecoveryCodesKey);

        public ManageController(
          DataContext context,
          UserManager<ApplicationUser> userManager,
          SignInManager<ApplicationUser> signInManager,
          IEmailSender emailSender,
          ILogger<ManageController> logger,
          IHostEnvironment environment,
          UrlEncoder urlEncoder)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _emailSender = emailSender;
            _logger = logger;
            _urlEncoder = urlEncoder;
            _appEnvironment = environment;
        }

        [TempData]
        public string StatusMessage { get; set; }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new IndexViewModel
            {
                Username = user.UserName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                IsEmailConfirmed = user.EmailConfirmed,
                StatusMessage = StatusMessage
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var email = user.Email;
            if (model.Email != email)
            {
                var setEmailResult = await _userManager.SetEmailAsync(user, model.Email);
                if (!setEmailResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting email for user with ID '{user.Id}'.");
                }
            }

            var phoneNumber = user.PhoneNumber;
            if (model.PhoneNumber != phoneNumber)
            {
                var setPhoneResult = await _userManager.SetPhoneNumberAsync(user, model.PhoneNumber);
                if (!setPhoneResult.Succeeded)
                {
                    throw new ApplicationException($"Unexpected error occurred setting phone number for user with ID '{user.Id}'.");
                }
            }

            StatusMessage = "Your profile has been updated";
            return RedirectToAction(nameof(Index));
        }
        [HttpPost]
        public async Task<JsonResult> ProfileEdit(VmUser vm)
        {
            try
            {
                var user =await  _context.Users.Where(a => a.Id == vm.ID).Include(a=>a.TblImage).SingleOrDefaultAsync();
                FileRepository RepImg = new FileRepository();
                if (user.Email != null && vm.Email!=null)
                {
                    if (vm.Email != user.Email)
                    {
                        var EmailConfirm = _userManager.FindByEmailAsync(vm.Email).Result;
                        if (EmailConfirm != null)
                        {
                            return Json(new { result = "faill", msg = "ایمیل وارد شده تکراری می باشد" });
                        }
                    }
                }
                #region ProfileImage
                if (vm.Image != null)
                {
                    Random rnd = new Random();
                    Ftp MyFtp = new Ftp();
                    string FileName = "UserImg-"+user.UserName+ rnd.Next(10000) + ".jpg";
                    int FtpID = MyFtp.Upload("UserImg", FileName, vm.Image.OpenReadStream());
                    if (FtpID != -1)
                    {
                        TblImage tImage = new TblImage();
                        tImage.Alt= "تصویر" + " " + user.FirstName + user.LastName;
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

                            if (OldImageID != -1 && user.ImageID != 8 &&user.ImageID!=null)
                            {
                                var qImage = _context.TblImage.Where(a => a.ID == OldImageID).Include(a => a.TblServer).Single();
                                if (MyFtp.Remove(qImage.TblServer.ID, qImage.FileName))
                                {
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
                }
                #endregion ProfileImage
                if(!string.IsNullOrEmpty(vm.Email) && vm.Email != user.Email)
                {
                    //user.Status = 0;
                    user.Email = vm.Email;

                    //var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                    //var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);

                    //string EmailTemplate = "";
                    //using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "Email.html")))
                    //{
                    //    EmailTemplate = reader.ReadToEnd();
                    //}
                    //string text = "جهت فعالسازی حساب کاربری خود لینک زیر را کلیک نمایید";
                    //string Body = EmailTemplate.Replace("[TITLE]", "تغییر کلمه عبور").Replace("[TEXT]", text).Replace("[LINK]", HtmlEncoder.Default.Encode(callbackUrl));
                    //await _emailSender.SendEmailConfirmationAsync(vm.Email, callbackUrl, Body);
                }
                user.FirstName = vm.FirstName;
                user.LastName = vm.LastName;
                user.Address = vm.Address;
                user.PostalCode = vm.PostalCode;
                //user.ShansnamehNumber = vm.ShansnamehNumber;
                user.CodeMelli = vm.CodeMelli;
                user.Country = vm.Country;
                user.State = vm.State;
                user.City = vm.City;
                user.Description = vm.Description;
                user.PhoneNumber = vm.PhoneNumber;
                _context.Update(user);
                await _context.SaveChangesAsync();

                var Image = RepImg.GetImageByID(user.ImageID);
                string imageProfile= Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                string Fullname = user.FirstName + " " + user.LastName;
                return Json(new { result = "ok", msg = "تغییرات با موفقیت انجام گردید",img=imageProfile,name= Fullname });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطای غیر منتظره ای رخ داد" });
            }
        }

        public async Task<IActionResult> MobileEdit(string Mobile)
        {
            if (Mobile ==null || !IsPhoneNumber(Mobile))
            {
                return Json(new { result = "faill", msg = "تلفن همراه وارد شده نامعتبر می باشد" });
            }
            var qUser = _context.Users.Where(a => a.Mobile == Mobile).FirstOrDefault();
            if (qUser != null)
            {
                return Json(new { result = "faill", msg = "تلفن همراه وارد شده قبلا ثبت شده است" });
            }
            else
            {
                var user =await  _userManager.FindByIdAsync(User.GetUserID());
                //user.Mobile = Mobile;
                user.Status = 0;
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    Random Rnd = new Random();
                    int token = Rnd.Next(1000, 9999);
                    SmsSender sms = new SmsSender();
                    user.MobileConfirmCode = token.ToString()+","+Mobile;
                    await _userManager.UpdateAsync(user);
                    string smsText = "کد فعالسازی شماره همراه در آکادمی موسیقی آرمیس : " + token.ToString();
                    string mCodeSender = sms.SendSms(smsText, Mobile);

                    return Json(new { result = "ok", msg = "برای فعال سازی شماره همراه، لطفا کد ارسال شده به شماره همراه را وارد نمایید" });
                }
                else
                {
                    return Json(new { result = "faill", msg = "خطا در ذخیره سازی !" });
                }
            }
        }
        public async Task<IActionResult> MobileCodeSubmit(string Code)
        {
            string[] Value = Code.Split(new string[] { "," }, StringSplitOptions.None);
            var user = await _userManager.FindByIdAsync(User.GetUserID());
            if (Value[0] != "" || Value[0] != Code)
            {
                return Json(new { result = "faill", msg = "کد فعالسازی نامعتبر می باشد" });
            }
            else
            {
                user.Status = 1;
                user.Mobile = Value[1];
                await _userManager.UpdateAsync(user);
                return Json(new { result = "ok", msg = "شماره همراه با موفقیت تغییر پیدا کرد" });
            }
        }
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"(\+98|0)?9\d{9}").Success;
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendVerificationEmail(IndexViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var code = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var callbackUrl = Url.EmailConfirmationLink(user.Id, code, Request.Scheme);
            var email = user.Email;
            await _emailSender.SendEmailConfirmationAsync(email, callbackUrl,"");

            StatusMessage = "Verification email sent. Please check your email.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (model.NewPassword!=model.ConfirmPassword)
            {
                return Json(new { result = "faill", msg = "کلمه های عبور با هم مطابقت ندارند" });
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var changePasswordResult = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
            if (!changePasswordResult.Succeeded)
            {
                AddErrors(changePasswordResult);
                return Json(new { result = "faill", msg = "کلمه عبور فعلی را به درستی وارد نکرده اید" });
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            _logger.LogInformation("User changed their password successfully.");
            StatusMessage = "Your password has been changed.";
            return Json(new { result = "ok", msg = "کلمه عبور شما با موفقیت بروزرسانی شد" });
        }

        [HttpGet]
        public async Task<IActionResult> SetPassword()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var hasPassword = await _userManager.HasPasswordAsync(user);

            if (hasPassword)
            {
                return RedirectToAction(nameof(ChangePassword));
            }

            var model = new SetPasswordViewModel { StatusMessage = StatusMessage };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetPassword(SetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var addPasswordResult = await _userManager.AddPasswordAsync(user, model.NewPassword);
            if (!addPasswordResult.Succeeded)
            {
                AddErrors(addPasswordResult);
                return View(model);
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "Your password has been set.";

            return RedirectToAction(nameof(SetPassword));
        }
        public JsonResult SocialEdit(TblUserSocial t)
        {
            try
            {
                var user = _userManager.FindByIdAsync(User.GetUserID()).Result;
                var qSocial = _context.TblUserSocial.Where(a => a.UserID == user.Id).SingleOrDefault();
                if (user.SocialID != null)
                {
                    qSocial.SkypeID = t.SkypeID;
                    qSocial.LinkedinID = t.LinkedinID;
                    qSocial.TelegramID = t.TelegramID;
                    qSocial.InstagramID = t.InstagramID;
                    _context.Update(qSocial);
                    _context.SaveChanges();
                }
                else
                {
                    t.UserID = User.GetUserID();
                    _context.Add(t);
                    _context.SaveChanges();
                    user.SocialID = t.ID;
                    _userManager.UpdateAsync(user);
                }
                return Json(new { result = "ok", msg = "اطلاعات احتماعی با موفقیت بروزرسانی گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "خطا در بروزرسانی. لطفا در زمان دیگری تلاش نمایید" });
            }
        }
        [HttpGet]
        public async Task<IActionResult> ExternalLogins()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new ExternalLoginsViewModel { CurrentLogins = await _userManager.GetLoginsAsync(user) };
            model.OtherLogins = (await _signInManager.GetExternalAuthenticationSchemesAsync())
                .Where(auth => model.CurrentLogins.All(ul => auth.Name != ul.LoginProvider))
                .ToList();
            model.ShowRemoveButton = await _userManager.HasPasswordAsync(user) || model.CurrentLogins.Count > 1;
            model.StatusMessage = StatusMessage;

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkLogin(string provider)
        {
            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            // Request a redirect to the external login provider to link a login for the current user
            var redirectUrl = Url.Action(nameof(LinkLoginCallback));
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl, _userManager.GetUserId(User));
            return new ChallengeResult(provider, properties);
        }

        [HttpGet]
        public async Task<IActionResult> LinkLoginCallback()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var info = await _signInManager.GetExternalLoginInfoAsync(user.Id);
            if (info == null)
            {
                throw new ApplicationException($"Unexpected error occurred loading external login info for user with ID '{user.Id}'.");
            }

            var result = await _userManager.AddLoginAsync(user, info);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred adding external login for user with ID '{user.Id}'.");
            }

            // Clear the existing external cookie to ensure a clean login process
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);

            StatusMessage = "The external login was added.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLogin(RemoveLoginViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var result = await _userManager.RemoveLoginAsync(user, model.LoginProvider, model.ProviderKey);
            if (!result.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occurred removing external login for user with ID '{user.Id}'.");
            }

            await _signInManager.SignInAsync(user, isPersistent: false);
            StatusMessage = "The external login was removed.";
            return RedirectToAction(nameof(ExternalLogins));
        }

        [HttpGet]
        public async Task<IActionResult> TwoFactorAuthentication()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new TwoFactorAuthenticationViewModel
            {
                HasAuthenticator = await _userManager.GetAuthenticatorKeyAsync(user) != null,
                Is2faEnabled = user.TwoFactorEnabled,
                RecoveryCodesLeft = await _userManager.CountRecoveryCodesAsync(user),
            };

            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Disable2faWarning()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!user.TwoFactorEnabled)
            {
                throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
            }

            return View(nameof(Disable2fa));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Disable2fa()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var disable2faResult = await _userManager.SetTwoFactorEnabledAsync(user, false);
            if (!disable2faResult.Succeeded)
            {
                throw new ApplicationException($"Unexpected error occured disabling 2FA for user with ID '{user.Id}'.");
            }

            _logger.LogInformation("User with ID {UserId} has disabled 2fa.", user.Id);
            return RedirectToAction(nameof(TwoFactorAuthentication));
        }

        [HttpGet]
        public async Task<IActionResult> EnableAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            var model = new EnableAuthenticatorViewModel();
            await LoadSharedKeyAndQrCodeUriAsync(user, model);

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EnableAuthenticator(EnableAuthenticatorViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!ModelState.IsValid)
            {
                await LoadSharedKeyAndQrCodeUriAsync(user, model);
                return View(model);
            }

            // Strip spaces and hypens
            var verificationCode = model.Code.Replace(" ", string.Empty).Replace("-", string.Empty);

            var is2faTokenValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, _userManager.Options.Tokens.AuthenticatorTokenProvider, verificationCode);

            if (!is2faTokenValid)
            {
                ModelState.AddModelError("Code", "Verification code is invalid.");
                await LoadSharedKeyAndQrCodeUriAsync(user, model);
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);
            _logger.LogInformation("User with ID {UserId} has enabled 2FA with an authenticator app.", user.Id);
            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            TempData[RecoveryCodesKey] = recoveryCodes.ToArray();

            return RedirectToAction(nameof(ShowRecoveryCodes));
        }

        [HttpGet]
        public IActionResult ShowRecoveryCodes()
        {
            var recoveryCodes = (string[])TempData[RecoveryCodesKey];
            if (recoveryCodes == null)
            {
                return RedirectToAction(nameof(TwoFactorAuthentication));
            }

            var model = new ShowRecoveryCodesViewModel { RecoveryCodes = recoveryCodes };
            return View(model);
        }

        [HttpGet]
        public IActionResult ResetAuthenticatorWarning()
        {
            return View(nameof(ResetAuthenticator));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetAuthenticator()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            await _userManager.SetTwoFactorEnabledAsync(user, false);
            await _userManager.ResetAuthenticatorKeyAsync(user);
            _logger.LogInformation("User with id '{UserId}' has reset their authentication app key.", user.Id);

            return RedirectToAction(nameof(EnableAuthenticator));
        }

        [HttpGet]
        public async Task<IActionResult> GenerateRecoveryCodesWarning()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!user.TwoFactorEnabled)
            {
                throw new ApplicationException($"Cannot generate recovery codes for user with ID '{user.Id}' because they do not have 2FA enabled.");
            }

            return View(nameof(GenerateRecoveryCodes));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateRecoveryCodes()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                throw new ApplicationException($"Unable to load user with ID '{_userManager.GetUserId(User)}'.");
            }

            if (!user.TwoFactorEnabled)
            {
                throw new ApplicationException($"Cannot generate recovery codes for user with ID '{user.Id}' as they do not have 2FA enabled.");
            }

            var recoveryCodes = await _userManager.GenerateNewTwoFactorRecoveryCodesAsync(user, 10);
            _logger.LogInformation("User with ID {UserId} has generated new 2FA recovery codes.", user.Id);

            var model = new ShowRecoveryCodesViewModel { RecoveryCodes = recoveryCodes.ToArray() };

            return View(nameof(ShowRecoveryCodes), model);
        }

        #region Helpers

        private void AddErrors(IdentityResult result)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }

        private string FormatKey(string unformattedKey)
        {
            var result = new StringBuilder();
            int currentPosition = 0;
            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }
            if (currentPosition < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition));
            }

            return result.ToString().ToLowerInvariant();
        }

        private string GenerateQrCodeUri(string email, string unformattedKey)
        {
            return string.Format(
                AuthenticatorUriFormat,
                _urlEncoder.Encode("ArmisApp"),
                _urlEncoder.Encode(email),
                unformattedKey);
        }

        private async Task LoadSharedKeyAndQrCodeUriAsync(ApplicationUser user, EnableAuthenticatorViewModel model)
        {
            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            model.SharedKey = FormatKey(unformattedKey);
            model.AuthenticatorUri = GenerateQrCodeUri(user.Email, unformattedKey);
        }

        #endregion
    }
}
