using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.ExMethod;
using ArmisApp.Models.Repository;
using ArmisApp.Models.Utility;
using ArmisApp.Models.ViewModels;
using ArmisApp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using Parbad;
using Parbad.AspNetCore;
using Microsoft.Extensions.Hosting;
using Parbad.Gateway.Melli;
using Parbad.Gateway.ParbadVirtual;

namespace ArmisApp.Controllers
{
    public class HomeController : Controller
    {
        private readonly IHostEnvironment _appEnvironment;
        private readonly DataContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HomeController> _logger;
        private IHttpContextAccessor _accessor;
        private readonly IOnlinePayment _onlinePayment;

        private HttpClient _client { get; } = new HttpClient();

        public HomeController(
            DataContext context,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<HomeController> logger,
            IHttpContextAccessor accessor,
            IHostEnvironment environment,
            IOnlinePayment onlinePayment)
        {
            _context = context;
            _emailSender = emailSender;
            _logger = logger;
            _configuration = configuration;
            _accessor = accessor;
            _appEnvironment = environment;
            _onlinePayment = onlinePayment;

        }
        [Route("")]
        public IActionResult Index()
        {
            ToolsRepository Rep_tools = new ToolsRepository();
            //ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            ViewBag.MetaDesc = Rep_tools.Settings().Description;
            ViewBag.Canonical = "https://" + Request.Host;

            ViewBag.currentTab = "home";
            return View();
        }

        public IActionResult Index3()
        {
            ToolsRepository Rep_tools = new ToolsRepository();
            //ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            ViewBag.MetaDesc = Rep_tools.Settings().Description;
            ViewBag.Canonical = "https://" + Request.Host;

            string _siteVersion = "4.2.11";
            ViewBag.Version = _siteVersion;
            //string cookieValue = Request.Cookies["e_link"];
            //if (cookieValue == null || cookieValue != _siteVersion)
            //{
            //    SetCookie("site_ver", _siteVersion, 90);
            //}
            ViewBag.currentTab = "home";
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> Test()
        {
            #region Notices To Admin
            string AdminEmail = "dereny24@outlook.com";

            string EmailTemplate = "";
            using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email.html")))
            {
                EmailTemplate = reader.ReadToEnd();
            }
            //string text = "جهت مشاهده جزئیات تیکت بر روی مشاهده کلیک نمایید";
            string Body = EmailTemplate.Replace("[TITLE]", "تیکت جدید در آرمیس آکادمی").Replace("[TEXT]", "متن تست")
                .Replace("[SENDER]", "alireza")
                .Replace("[COURSE]", "-")
                .Replace("[LINK]", "تست").Replace("[LINK-TITLE]", "مشاهده");
            await _emailSender.SendEmailNoticAsync(AdminEmail, "تیکت جدید", Body);
            #endregion
            return Content("ok");
        }
        public async Task<IActionResult> MelliTest(string ID = "")
        {
            //var melli = await _onlinePayment.RequestAsync("Melli", (long)(int)(1734),Convert.ToDecimal(1000), "https://armisacademy.com/Transactions/VerifyMelli",collectionto);
            if (ID == "test")
            {
                var melli2 = await _onlinePayment.RequestAsync(invoice =>
                {
                    invoice
                       .SetAmount((long)(int)5000 * 10)
                       .UseAutoIncrementTrackingNumber()
                       .SetCallbackUrl("https://armisacademy.com/VerifyMelli")
                       .UseMelli()
                       .UseParbadVirtual();
                });
                if (melli2.IsSucceed)
                {
                    return melli2.GatewayTransporter.TransportToGateway();
                }
                else
                {
                    return Content(melli2.Message);
                }
            }
            else
            {
                var melli = await _onlinePayment.RequestAsync(invoice =>
                {
                    invoice
                       .UseAutoRandomTrackingNumber()
                       .SetAmount((long)(int)5000 * 10)
                       .SetCallbackUrl("https://armisacademy.com/VerifyMelli")
                       .UseMelli();

                    //.UseParbadVirtual();
                });
                if (melli.IsSucceed)
                {
                    // Save the TrackingNumber inside your database.
                    // It will redirect the client to the gateway.
                    return melli.GatewayTransporter.TransportToGateway();
                }
                else
                {
                    // The request was not successful. You can see the Message property for more information.
                    //"خطا در اتصال به درگاه ! لطفا از یک درگاه دیگر استفاده نمایید";
                    return Content(melli.Message);
                }
            }
        }

        public async Task<IActionResult> VerifyMelli()
        {
            var invoice = await _onlinePayment.FetchAsync();

            if (invoice.Status == PaymentFetchResultStatus.AlreadyProcessed)
            {
                var isAlreadyVerified = invoice.IsAlreadyVerified;
                return Content("The payment is already processed before.");
            }
            var verifyResult = await _onlinePayment.VerifyAsync(invoice);
            if (verifyResult.IsSucceed)
            {
                return Content("IsSucceed");
            }
            else
            {
                return Content("IsFailed");
            }

        }
        public IActionResult CoursesList(int[] GroupID, string[] Level, string[] Prq, int Page = 1, string Sort = "newest",string s="")
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            var qCourse = _context.TblCourse.Where(a => a.Status == 1 || a.Status == 2).AsQueryable();

            List<VmCourses> lstCourse = new List<VmCourses>();
            CourseRepository RepCourse = new CourseRepository();
            ToolsRepository Rep_tools = new ToolsRepository();

            qCourse.Include(a => a.TblUser).Load();
            qCourse.Include(a => a.TblGroup).Load();
            qCourse.Include(a => a.TblLevelPrice).Load();
            qCourse.Include(a => a.TblImage).ThenInclude(a => a.TblServer).Load();
            qCourse = qCourse.OrderByDescending(a => a.Date);
            bool filtered = false;
            if (!string.IsNullOrEmpty(s))
            {
                qCourse = qCourse.Where(a => a.Title.Contains(s));
                var DataList = RepCourse.GetCourseListData(qCourse);
                lstCourse.AddRange(DataList);

                filtered = true;
            }
            if (GroupID.Count() > 0)
            {
                for (int i = 0; i < GroupID.Count(); i++)
                {
                    qCourse = qCourse.Where(a => a.TblGroup.GroupID == GroupID[i]);
                    var DataList = RepCourse.GetCourseListData(qCourse);
                    lstCourse.AddRange(DataList);
                }
                filtered = true;
            }
            if (Level.Count() > 0)
            {
                for (int i = 0; i < Level.Count(); i++)
                {
                    if (Level[i] != null)
                    {
                        qCourse = qCourse.Where(a => a.CourseLevel == Level[i]);
                        var DataList = RepCourse.GetCourseListData(qCourse);
                        lstCourse.AddRange(DataList);
                        filtered = true;
                    }
                }

            }
            if (Prq.Count() > 0)
            {
                for (int i = 0; i < Prq.Count(); i++)
                {
                    if (Prq[i] != null)
                    {
                        qCourse = qCourse.Where(a => a.Prerequisites == Prq[i]);
                        var DataList = RepCourse.GetCourseListData(qCourse);
                        lstCourse.AddRange(DataList);
                        filtered = true;
                    }
                }
            }
            if(!filtered)
            {
                var DataList = RepCourse.GetCourseListData(qCourse);
                lstCourse.AddRange(DataList);
            }
            int Take = 10;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = lstCourse.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            switch (Sort)
            {
                case "oldest":
                    lstCourse = lstCourse.OrderBy(a => a.NormalDate).ToList();
                    break;
                case "cheapest":
                    lstCourse = lstCourse.OrderBy(a => a.FinalPrice).ToList();
                    break;
                case "expensive":
                    lstCourse = lstCourse.OrderByDescending(a => a.FinalPrice).ToList();
                    break;
                case "visited":
                    lstCourse = lstCourse.OrderByDescending(a => a.Visit).ToList();
                    break;
            }

            lstCourse = lstCourse.Skip(Skip).Take(Take).ToList();

            ViewData["MainGroups"] = _context.TblGroup.Where(a => a.GroupID != 0).Include(a => a.TblCourse).ToList();
            ViewBag.Canonical = "https://" + Request.Host + "/CoursesList";
            ViewBag.currentTab = "course";
            ViewBag.MetaDesc = Rep_tools.Settings().Description;
            ViewBag.mainBreadcrumb = "دوره های آموزشی";

            return View(lstCourse);
        }
        public IActionResult CoursesList2(int[] GroupID, string[] Level, string[] Prq, int Page = 1, string Sort = "newest", string s = "")
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            var qCourse = _context.TblCourse.Where(a => a.Status == 1 || a.Status == 2).AsQueryable();

            List<VmCourses> lstCourse = new List<VmCourses>();
            CourseRepository RepCourse = new CourseRepository();
            ToolsRepository Rep_tools = new ToolsRepository();

            qCourse.Include(a => a.TblUser).Load();
            qCourse.Include(a => a.TblGroup).Load();
            qCourse.Include(a => a.TblLevelPrice).Load();
            qCourse.Include(a => a.TblImage).ThenInclude(a => a.TblServer).Load();
            qCourse = qCourse.OrderByDescending(a => a.Date);
            bool filtered = false;
            if (!string.IsNullOrEmpty(s))
            {
                qCourse = qCourse.Where(a => a.Title.Contains(s));
                var DataList = RepCourse.GetCourseListData(qCourse);
                lstCourse.AddRange(DataList);

                filtered = true;
            }
            if (GroupID.Count() > 0)
            {
                for (int i = 0; i < GroupID.Count(); i++)
                {
                    qCourse = qCourse.Where(a => a.TblGroup.GroupID == GroupID[i]);
                    var DataList = RepCourse.GetCourseListData(qCourse);
                    lstCourse.AddRange(DataList);
                }
                filtered = true;
            }
            if (Level.Count() > 0)
            {
                for (int i = 0; i < Level.Count(); i++)
                {
                    if (Level[i] != null)
                    {
                        qCourse = qCourse.Where(a => a.CourseLevel == Level[i]);
                        var DataList = RepCourse.GetCourseListData(qCourse);
                        lstCourse.AddRange(DataList);
                        filtered = true;
                    }
                }

            }
            if (Prq.Count() > 0)
            {
                for (int i = 0; i < Prq.Count(); i++)
                {
                    if (Prq[i] != null)
                    {
                        qCourse = qCourse.Where(a => a.Prerequisites == Prq[i]);
                        var DataList = RepCourse.GetCourseListData(qCourse);
                        lstCourse.AddRange(DataList);
                        filtered = true;
                    }
                }
            }
            if (!filtered)
            {
                var DataList = RepCourse.GetCourseListData(qCourse);
                lstCourse.AddRange(DataList);
            }
            int Take = 10;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = lstCourse.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            switch (Sort)
            {
                case "oldest":
                    lstCourse = lstCourse.OrderBy(a => a.NormalDate).ToList();
                    break;
                case "cheapest":
                    lstCourse = lstCourse.OrderBy(a => a.FinalPrice).ToList();
                    break;
                case "expensive":
                    lstCourse = lstCourse.OrderByDescending(a => a.FinalPrice).ToList();
                    break;
                case "visited":
                    lstCourse = lstCourse.OrderByDescending(a => a.Visit).ToList();
                    break;
            }

            lstCourse = lstCourse.Skip(Skip).Take(Take).ToList();

            ViewData["MainGroups"] = _context.TblGroup.Where(a => a.GroupID != 0).Include(a => a.TblCourse).ToList();
            ViewBag.Canonical = "https://" + Request.Host + "/CoursesList";
            ViewBag.currentTab = "course";
            ViewBag.MetaDesc = Rep_tools.Settings().Description;
            ViewBag.mainBreadcrumb = "دوره های آموزشی";

            return View(lstCourse);
        }

        public IActionResult CourseListFilter(int[] GroupID,string[] Level, string[] Prq, int Page=1, string Sort="newest")
        {
            var qCourse = _context.TblCourse.Where(a => a.Status == 1 || a.Status == 2).AsQueryable();

            qCourse.Include(a => a.TblUser).Load();
            qCourse.Include(a => a.TblGroup).Load();
            qCourse.Include(a => a.TblLevelPrice).Load();
            qCourse.Include(a => a.TblImage).ThenInclude(a => a.TblServer).Load();
            qCourse = qCourse.OrderByDescending(a => a.Date);

            List<VmCourses> lstCourse = new List<VmCourses>();
            CourseRepository RepCourse = new CourseRepository();
            //qCourse = qCourse.Skip(Skip).Take(Take);
            if (GroupID.Count() > 0)
            {
                for (int i = 0; i < GroupID.Count(); i++)
                {
                    qCourse = qCourse.Where(a => a.TblGroup.GroupID == GroupID[i]);
                    var DataList = RepCourse.GetCourseListData(qCourse);
                    lstCourse.AddRange(DataList);
                }
            }
            if (Level.Count() > 0)
            {
                for (int i = 0; i < Level.Count(); i++)
                {
                    qCourse = qCourse.Where(a => a.CourseLevel == Level[i]);
                    var DataList = RepCourse.GetCourseListData(qCourse);
                    lstCourse.AddRange(DataList);
                }
            }
            if (Prq.Count() > 0)
            {
                for (int i = 0; i < Prq.Count(); i++)
                {
                    qCourse = qCourse.Where(a => a.Prerequisites == Prq[i]);
                    var DataList = RepCourse.GetCourseListData(qCourse);
                    lstCourse.AddRange(DataList);
                }
            }
            else if(GroupID.Count() == 0 && Level.Count() == 0 && Prq.Count() == 0)
            {
                var DataList = RepCourse.GetCourseListData(qCourse);
                lstCourse.AddRange(DataList);
            }
            int Take = 10;
            Page = Page <= 0 ? 1 : Page;
            int CountStatus = lstCourse.Count();
            Page = Page > (int)Math.Ceiling((decimal)CountStatus / Take) ? (int)Math.Ceiling((decimal)CountStatus / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)CountStatus / Take);

            switch (Sort)
            {
                case "oldest":
                    lstCourse = lstCourse.OrderBy(a => a.NormalDate).ToList();
                    break;
                case "cheapest":
                    lstCourse = lstCourse.OrderBy(a => a.FinalPrice).ToList();
                    break;
                case "expensive":
                    lstCourse = lstCourse.OrderByDescending(a => a.FinalPrice).ToList();
                    break;
                case "visited":
                    lstCourse = lstCourse.OrderByDescending(a => a.Visit).ToList();
                    break;
            }

            return PartialView("P_CourseList",lstCourse.Skip(Skip).Take(Take).ToList());
        }

        [Route("CoursePreview3/{title}/{id}")]
        public IActionResult CoursePreview3(int ID, string Title)
        {
            ViewBag.currentTab = "CoursesList";
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            string UserID = User.GetUserID();
            TimeUtility Time = new TimeUtility();
            string NewTitle = Title.Replace("-", " ");
            var Course = _context.TblCourse.Where(a => a.ID == ID).Where(a => a.ShortLink == NewTitle);
            if (Course.Count() <= 0)
            {
                return RedirectToAction(nameof(Er404));
            }

            Course.Include(a => a.TblUser).Load();
            Course.Include(a => a.TblGroup).Load();
            Course.Include(a => a.TblSession).Load();
            Course.Include(a => a.TblUserLevels).Load();
            Course.Include(a => a.TblLevelPrice).Load();
            Course.Include(a => a.TblComment).ThenInclude(a => a.TblUserSender).Load();
            var qCourse = Course.SingleOrDefault();

            ViewBag.TotalSession = qCourse.TblSession.Count().ToString().ConvertNumerals();
            var qUserCourse = _context.TblUserCourse.Where(a => a.CourseID == qCourse.ID).ToList();

            FileRepository RepImg = new FileRepository();
            CourseRepository RepCourse = new CourseRepository();
            VmCourses vm = new VmCourses();

            vm.TeacherUserName = qCourse.TblUser.UserName;
            vm.TeacherInfo = qCourse.TblUser.Description;
            vm.ID = qCourse.ID;
            vm.GroupName = qCourse.TblGroup.Title;
            vm.CIntroductory = qCourse.CIntroductory;
            vm.Link = "/Course/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID;
            vm.ShortLink = "/CoursePreview/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID;
            vm.Teacher = qCourse.TblUser.FirstName + " " + qCourse.TblUser.LastName;
            vm.TeacherID = qCourse.TblUser.UserName;
            vm.Title = qCourse.Title;
            vm.Text = qCourse.Text;
            vm.CourseLevel = qCourse.CourseLevel;
            vm.InSupport = qCourse.InSupport;
            vm.DownloadFileCount = qCourse.DownloadFileCount;
            vm.Prerequisites = qCourse.Prerequisites;
            vm.Description = qCourse.Description;
            if (qCourse.ISMNCode != null)
            {
                vm.ISMNCode = qCourse.ISMNCode.ToString().ConvertNumerals();
            }
            vm.FullTime = RepCourse.GetCourseTime(qCourse.ID);
            vm.Date = qCourse.Date.ToShamsi().ToString("yyyy/MM/dd");
            vm.LastUpdate = Time.GetTimeName(qCourse.LastUpdate).ToString().ConvertNumerals();
            vm.MetaTag = qCourse.Keywords;

            var qBuyer = qUserCourse.Where(a => a.UserID == UserID).FirstOrDefault();
            if (qBuyer != null)
            {
                vm.UserBuyer = true;
            }
            if (qCourse.TblLevelPrice.Count() > 0)
            {
                int TotalPrice = qCourse.TblLevelPrice.Sum(a => a.Prise);
                vm.BasePrice = qCourse.TblLevelPrice.FirstOrDefault().Prise.ToString("N0").ConvertNumerals();
                vm.TotalPrice = TotalPrice.ToString("N0").ConvertNumerals();
                if (qCourse.DiscountPercent > 0)
                {
                    //محاسبه تخفیف بر اساس درصد
                    int DiscountPrice = ((qCourse.DiscountPercent * (TotalPrice)) / 100).Value;
                    vm.Price = (TotalPrice - DiscountPrice).ToString("N0").ConvertNumerals();

                    ViewBag.Price = (TotalPrice - DiscountPrice) * 10;
                }
                else
                {
                    ViewBag.Price = qCourse.TblLevelPrice.FirstOrDefault().Prise * 10;
                }
            }
            #region امتیاز دهی

            vm.CourseRating = new CourseRating();
            var qRating = RepCourse.GetCourseRating(qCourse.ID);
            vm.CourseRating.Rating = qRating.Rating;
            vm.CourseRating.RatingCount = qRating.RatingCount;
            vm.CourseRating.FiveStar = qRating.FiveStar;
            vm.CourseRating.FourStar = qRating.FourStar;
            vm.CourseRating.ThreeStar = qRating.ThreeStar;
            vm.CourseRating.TwoStar = qRating.TwoStar;
            vm.CourseRating.OneStar = qRating.OneStar;

            #endregion
            var TeacherImage = RepImg.GetImageByID(qCourse.TblUser.ImageID);
            vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;

            if (qCourse.ImageID != null)
            {
                var Image = RepImg.GetImageByID(qCourse.ImageID);
                vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
            }
            vm.Session = new Session();
            int levelCount = qCourse.CIntroductory; /*+ qCourse.CMedium + qCourse.CAdvanced;*/
            vm.LstLevel = new List<Levels>();

            #region پرسش های متداول دوره
            vm.LstFAQ = new List<VmCourseFAQ>();
            var qGoude = _context.TblGuide.Where(a => a.Type == 6)
                .OrderByDescending(a => a.ID).OrderBy(a => a.Sort).ToList();
            foreach (var item in qGoude)
            {
                if (qCourse.QuestionsList != null)
                {
                    VmCourseFAQ fq = new VmCourseFAQ();
                    string[] faq = qCourse.QuestionsList.Split(new char[] { ',' });
                    foreach (var itemSub in faq)
                    {
                        if (itemSub != "" && Convert.ToInt32(itemSub) == item.ID)
                        {
                            fq.ID = item.ID;
                            fq.Text = item.Text;
                            fq.Title = item.Title;

                            vm.LstFAQ.Add(fq);
                        }
                    }
                }
            }
            #endregion
            // نمایش قسمت محتوای دوره
            #region محتوای دوره
            bool IsAdminRole = User.IsInRole("Admin");
            ViewBag.TotalVideo = RepCourse.TotalVideo(qCourse.ID).ToString().ConvertNumerals();
            for (int i = 1; i < levelCount + 1; i++)
            {
                Levels lv = new Levels();
                var se = qCourse.TblSession.Where(a => a.Level == i)
                    .OrderBy(a => a.SessionSort)
                    .ToList();
                // قیمت سطح
                var lPrice = qCourse.TblLevelPrice.Where(a => a.Level == i).SingleOrDefault();
                if (lPrice != null)
                {
                    if (!string.IsNullOrEmpty(lPrice.Title))
                    {
                        lv.Title = lPrice.Title;
                    }
                    else
                    {
                        lv.Title = "سطح" + " " + i;
                    }
                }
                else
                {
                    lv.Title = "سطح" + " " + i;
                }
                if (se.Count() <= 0)
                {
                    lv.IsPending = true;
                }
                lv.Number = i;
                if (se.Count > 0)
                {
                    lv.LstSession = new List<Session>();
                    foreach (var item in se)
                    {
                        Session seItem = new Session();
                        seItem.ID = item.ID;
                        seItem.Sort = item.SessionSort;
                        seItem.Title = item.Title;
                        seItem.Description = item.Description;
                        seItem.IsFree = item.IsFree;
                        seItem.Price = item.Price;
                        lv.LstSession.Add(seItem);
                    }
                }
                //###########
                vm.LstLevel.Add(lv);
            }
            #endregion
            #region نمایش دیدگاه ها
            vm.Session.LstComments = new List<SessionComments>();
            ViewBag.BlogReviewScore = _context.TblScore.Where(a => a.TitleEn == "CourseReview").SingleOrDefault().Value;
            foreach (var item in qCourse.TblComment.Where(a => a.Status == 1).OrderByDescending(a => a.Date))
            {
                SessionComments sc = new SessionComments();
                sc.ID = item.ID;
                sc.Link = "/Profile/" + item.TblUserSender.UserName;
                sc.Text = item.Text;
                sc.Date = Time.GetDateName(item.Date).ToString().ConvertNumerals();
                sc.MiladiDate = item.Date.ToString("yyyy-MM-dd");
                if (item.BuyOffer != null)
                {
                    sc.BuyOffer = (int)item.BuyOffer;
                }
                sc.FullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                sc.UserName = item.TblUserSender.UserName;
                var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                if (item.UserSender == UserID)
                {
                    sc.IsMe = true;
                }
                if (item.ReplyID > 0)
                {
                    sc.ReplyID = (int)item.ReplyID;
                    var qReply = qCourse.TblComment.Where(a => a.ID == item.ReplyID).SingleOrDefault();
                    if (qReply != null)
                    {
                        sc.ReplyTo = qReply.TblUserSender.FirstName + " " + qReply.TblUserSender.LastName;
                    }
                }
                var qReview = _context.TblCourseReview.Where(a => a.CourseID == qCourse.ID).Where(a => a.UserID == item.UserSender).FirstOrDefault();
                if (qReview != null)
                {
                    sc.Rating = (decimal)qReview.Rating;
                }
                var qUserBuyer = qUserCourse.Where(a => a.UserID == item.UserSender).FirstOrDefault();
                if (qUserBuyer != null)
                {
                    sc.isBuyer = true;
                }

                vm.Session.LstComments.Add(sc);
            }
            #endregion
            ViewBag.Canonical = "https://" + (Request.Host + "/CoursePreview/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID);
            ViewBag.currentTab = "course";
            ViewBag.MetaDesc = qCourse.MetaDescription;
            return View(vm);
        }
        [Route("Course/{title}/{id}")]
        public IActionResult Course(int ID, string Title, int Level = 1, int SessionID = 0)
        {
            if (Title != null)
            {
                return RedirectToAction(nameof(CoursePreview), new { ID = ID , Title = Title });
            }
            ViewBag.currentTab = "CoursesList";
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            string NewTitle = Title.Replace("-", " ");
            var Course = _context.TblCourse.Where(a => a.ID == ID);

            Course.Include(a => a.TblUser).Load();
            Course.Include(a => a.TblGroup).Load();
            Course.Include(a => a.TblSession).ThenInclude(a => a.TblVideo).Load();
            Course.Include(a => a.TblUserLevels).Load();
            Course.Include(a => a.TblLevelPrice).Load();
            Course.Include(a => a.TblUserCourse).Load();
            var qCourse = Course.SingleOrDefault();
            qCourse.Visit += 1;
            _context.Update(qCourse);
            _context.SaveChanges();
            if (qCourse == null)
            {
                return null;
            }
            string UserID = User.GetUserID();
            TimeUtility Time = new TimeUtility();
            var qFirstSession = qCourse.TblSession.Where(a => a.Level == Level).OrderBy(a => a.SessionSort)
                    .FirstOrDefault();
            if (qFirstSession != null)
            {
                if (SessionID <= 0)
                {
                    SessionID = qFirstSession.ID;
                }
                //if (Level <= 0)
                //{
                //    Level = qFirstSession.Level;
                //}
            }
            FileRepository RepImg = new FileRepository();
            VmCourses vm = new VmCourses();
            vm.TeacherUserName = qCourse.TblUser.UserName;
            vm.ID = qCourse.ID;
            vm.GroupName = qCourse.TblGroup.Title;
            vm.CIntroductory = qCourse.CIntroductory;
            //vm.CMedium = qCourse.CMedium;
            //vm.CAdvanced = qCourse.CAdvanced;
            vm.Link = "/CoursePreview/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID;
            vm.Teacher = qCourse.TblUser.FirstName + " " + qCourse.TblUser.LastName;
            vm.Title = qCourse.Title;
            vm.Date = qCourse.Date.ToShamsi().ToString("yyyy/MM/dd");
            vm.LastUpdate = Time.GetTimeName(qCourse.LastUpdate);
            vm.DiscountDescription = qCourse.DiscountDescription;
            vm.Placement = qCourse.Placement;
            var qChat = _context.TblMessages.Where(a => a.SenderID == UserID).Where(a => a.CourseID == qCourse.ID)
                .OrderByDescending(a => a.Date).FirstOrDefault();
            if (qChat != null && qChat.ChatID != null)
            {
                vm.ChatID = (int)qChat.ChatID;
            }

            var TeacherImage = RepImg.GetImageByID(qCourse.TblUser.ImageID);
            vm.Image = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;

            int levelCount = qCourse.CIntroductory; /*+ qCourse.CMedium + qCourse.CAdvanced;*/
            vm.LstLevel = new List<Levels>();
            // نمایش قسمت محتوای دوره

            bool IsAdminRole = User.IsInRole("Admin");
            // ##### بررسی خرید کل دوره : 1000
            var qFullBuy = qCourse.TblUserCourse.Where(a=>a.UserID == UserID).Where(a => a.LevelID == 1000).SingleOrDefault();
            if (qFullBuy != null)
            {
                ViewBag.FullBuy = true;
            }
            //#####
            #region بررسی خرید کل و قیمت
            // قیمت کل و تخفیف
            int UserLevelPrice = 0;
            if (qCourse.TblLevelPrice.Count() > 0)
            {
                int TotalPrice = qCourse.TblLevelPrice.Sum(a => a.Prise);
                vm.TotalPrice = TotalPrice.ToString("N0").ConvertNumerals();

                vm.CourseDiscount = new CourseDiscount();
                vm.CourseDiscount.Discount = 0;

                var qUserLevels = qCourse.TblUserLevels.Where(a => a.UserID == UserID);

                // کسر سطح های خریداری شده یک کاربر از قیمت کل
                if (qUserLevels.Count() > 0)
                {
                    foreach (var item in qUserLevels)
                    {
                        var qLevelPrice = qCourse.TblLevelPrice.Where(a => a.Level == item.Level).FirstOrDefault();
                        if (qLevelPrice != null)
                        {
                            UserLevelPrice = UserLevelPrice + qLevelPrice.Prise;
                        }
                    }
                    vm.Price = (TotalPrice - UserLevelPrice).ToString("N0").ConvertNumerals();
                }

                if (qCourse.DiscountPercent > 0)
                {
                    //محاسبه تخفیف بر اساس درصد
                    int BuyPrice = TotalPrice - UserLevelPrice;
                    int DiscountPrice = ((qCourse.DiscountPercent * (BuyPrice)) / 100).Value;
                    int Finalprice = (BuyPrice - DiscountPrice);

                    #region محاسبه سطح های خریداری شده برای تخفیف کل دوره
                    // اگر محدودیت سطح وجود نداشت
                    if (qCourse.DiscountRemaining == null || qCourse.DiscountRemaining == 0)
                    {
                        vm.CourseDiscount.RemainingCount = -1000;
                        vm.CourseDiscount.LevelsPurchased = 0;
                    }
                    // اگر محدودیت سطح وجود داشت
                    else
                    {
                        vm.CourseDiscount.LevelsPurchased = qUserLevels.Count();
                        vm.CourseDiscount.RemainingCount = (int)qCourse.DiscountRemaining - vm.CourseDiscount.LevelsPurchased;
                        if (vm.CourseDiscount.RemainingCount < 0)
                        {
                            // لغو تخفیف بدلیل محدودیت سطح
                            Finalprice = BuyPrice;
                        }
                    }
                    #endregion
                    vm.Price = Finalprice.ToString("N0").ConvertNumerals();
                    vm.CourseDiscount.Discount = (int)qCourse.DiscountPercent;

                    if (Finalprice >= TotalPrice)
                    {
                        vm.Price = null;
                    }
                }
                ViewBag.price = qCourse.TblLevelPrice.FirstOrDefault().Prise * 10;
            }
            #endregion
            CourseRepository Rep_Course = new CourseRepository();
            for (int i = 1; i < levelCount + 1; i++)
            {
                Levels lv = new Levels();
                var se = qCourse.TblSession.Where(a => a.Level == i)
                    .OrderBy(a => a.SessionSort)
                    .ToList();
                lv.TotalVideo = Rep_Course.GetLevelTime(qCourse.ID, i).ID;
                lv.TotalTime = Rep_Course.GetLevelTime(qCourse.ID, i).Title.ToString().ConvertNumerals();
                #region قیمت سطح ها و و بررسی وضعیت خرید کاربر
                // قیمت سطح
                var lPrice = qCourse.TblLevelPrice.Where(a => a.Level == i).SingleOrDefault();
                //
                if (UserID == qCourse.TeacherID || IsAdminRole || qFullBuy != null)
                {
                    lv.IsBuy = true;
                }
                else
                {
                    // بررسی سطح های خریداری شده
                    var userBuy = qCourse.TblUserLevels.Where(a => a.Level == i).Where(a => a.UserID == UserID).FirstOrDefault();
                    //
                    if (qCourse.TblUserLevels.Count() > 0)
                    {
                        lv.IsBuy = userBuy != null ? true : false;
                    }
                    // نمایش سطح های رایگان
                    if (qCourse.TblLevelPrice.Count > 0)
                    {
                        if (userBuy == null)
                        {
                            if (lPrice != null)
                            {
                                lv.Price = lPrice.Prise.ToString("N0").ConvertNumerals() + " " + "تومان";
                                lv.Title = lPrice.Title;
                                lv.IsBuy = lPrice.Prise == 0 ? true : false;

                            }
                            else
                            {
                                lv.IsBuy = false;
                            }
                        }
                    }
                }
                #endregion
                #region LevelTitle
                if (lPrice != null)
                {
                    if (!string.IsNullOrEmpty(lPrice.Title))
                    {
                        lv.Title = lPrice.Title;
                    }
                    else
                    {
                        lv.Title = "سطح" + " " + i;
                    }
                }
                else
                {
                    lv.Title = "سطح" + " " + i;
                }
                if (se.Count() <= 0)
                {
                    lv.IsPending = true;
                }
                if (lPrice != null)
                {
                    lv.ISBNCode = lPrice.ISBNCode != null ? lPrice.ISBNCode.ToString().ConvertNumerals() : "";
                }
                lv.Number = i;
                #endregion LevelTitle
                //###### لیست جلسات ########
                string SecurityKey = "";
                if (se.Count > 0)
                {
                    lv.LstSession = new List<Session>();
                    foreach (var item in se)
                    {
                        Session seItem = new Session();
                        seItem.ID = item.ID;

                        seItem.Sort = item.SessionSort;
                        seItem.Title = item.Title;
                        seItem.Description = item.Description;
                        seItem.FileDescription = item.FileDescription;
                        seItem.IsFree = item.IsFree;
                        seItem.IsAccsess = false;
                        seItem.Price = item.Price;
                        seItem.ChatID = 0;
                        // نمایش جلسه برای خریداران سطح آموزشی
                        var qUserSBuy = _context.TblUserSession.Where(a => a.SessionID == item.ID)
                                .Where(a => a.UserID == UserID).FirstOrDefault();
                        //if (lv.IsBuy || qUserSBuy != null)
                        //{
                        //    seItem.IsFree = true;

                        //}
                        bool FreeSession = item.IsFree;
                        var LevelBuy = _context.TblUserLevels.Where(a => a.Level == i)
                        .Where(a => a.UserID == UserID).Where(a => a.CourseID == qCourse.ID).FirstOrDefault();

                        if (lv.IsBuy || LevelBuy != null || IsAdminRole || UserID == qCourse.TeacherID || FreeSession)
                        {
                            seItem.IsAccsess = true;

                            string IpAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                            //string test = "185.120.195.138";

                            var SecurityIp = IpAddress.ConvertScurity();
                            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("Armis" + "&" + SecurityIp));
                            SecurityKey = svcCredentials;
                        }
                        // نمایش لیست ویدئو های هر جلسه
                        seItem.LstVideo = new List<SessionVideos>();
                        TimeSpan FullTime = new TimeSpan();
                        string Number = "";
                        FileRepository RepVideo = new FileRepository();
                        foreach (var itemVid in item.TblVideo.OrderBy(a => a.Sort))
                        {
                            #region VideoTime
                            string Num = itemVid.Number.Value.ToString();
                            if (!Number.Contains(Num) || Number == "")
                            {
                                if (itemVid.Time != null && itemVid.Time.Contains(":"))
                                {
                                    string[] cTime = itemVid.Time.Split(new string[] { ":" }, StringSplitOptions.None);
                                    TimeSpan t1 = TimeSpan.Parse("00:" + cTime[0] + ":" + cTime[1]);

                                    FullTime += t1;
                                    Number += itemVid.Number + ",";
                                }
                            }
                            #endregion
                            SessionVideos sv = new SessionVideos();
                            if (item.ID == item.TblVideo.OrderBy(a => a.Sort).FirstOrDefault().ID)
                            {
                                vm.Session.FirstVideoID = item.ID;
                            }
                            sv.ID = itemVid.ID;
                            sv.Alt = itemVid.Alt;
                            sv.Title = itemVid.Title;
                            sv.Quality = itemVid.Quality;
                            sv.Number = (int)itemVid.Number; // شماره ترتیب ویدئو
                            sv.Sort = (int)itemVid.Sort;
                            sv.FileName = itemVid.FileName;
                            if (itemVid.PosterPath != null)
                            {
                                sv.Poster = itemVid.PosterPath;
                            }
                            else
                            {
                                sv.Poster = "/assets/images/course/course-preview.jpg";
                            }
                            if (itemVid.Url != null)
                            {
                                sv.dlLink = itemVid.Url + "?u=" + SecurityKey;
                                //sv.Link2 = "/vid/" + item.ID + "/" + item.Token;
                            }
                            else
                            {
                                var Video = RepVideo.GetVideoByID(itemVid.ID);
                                string Path = Video.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Video.TblServer.Path.Trim(new char[] { '/' }) + "/" + Video.FileName;
                                sv.dlLink = Path + "?u=" + SecurityKey;
                            }
                            seItem.FullTime = FullTime.ToString();
                            seItem.LstVideo.Add(sv);
                        }
                        lv.LstSession.Add(seItem);
                    }
                }
                //###########
                #region نمایش گفتگوها

                vm.LstConversion = new List<SessionConversion>();
                var qMessage = _context.TblMessages.Where(a => a.CourseID == qCourse.ID)
                    .Where(a => a.SenderID == UserID || a.ReaseverID == UserID)
                    .Include(a => a.TblSession)
                    .Include(a => a.TblUser)
                    .Include(a => a.TblFiles)
                    .OrderByDescending(a => a.Date)
                    .ToList();
                foreach (var item in qMessage)
                {
                    SessionConversion sc = new SessionConversion();
                    sc.ID = item.ID;
                    sc.Text = item.Text;
                    sc.Date = Time.GetDateName(item.Date);
                    sc.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                    var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                    sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    if (item.SenderID == UserID)
                    {
                        sc.IsMe = true;
                    }
                    if (item.TblFiles != null)
                    {
                        if (item.TblFiles.Token == null)
                        {
                            Guid originalGuid = Guid.NewGuid();
                            string stringGuids = originalGuid.ToString("N");
                            item.TblFiles.Token = stringGuids;
                            _context.Update(item.TblFiles);
                            _context.SaveChanges();
                        }
                        sc.File = new CMessageFile();
                        sc.File.ID = item.TblFiles.ID;
                        var file = RepImg.GetFileByID(item.TblFiles.ID);
                        //sc.File.Link = file.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + file.TblServer.Path.Trim(new char[] { '/' }) + "/" + file.FileName;
                        sc.File.Link = "/dl/file/" + item.TblFiles.ID + "/" + item.TblFiles.Token;
                        sc.File.Title = item.TblFiles.Title;
                    }
                    vm.LstConversion.Add(sc);
                }
                #endregion

                vm.LstLevel.Add(lv);
            }
            ViewBag.MetaTag = qCourse.MetaTag;
            ViewBag.BKeywords = qCourse.Keywords;
            ViewBag.CatName = qCourse.Title;
            ViewBag.MetaDesc = qCourse.MetaDescription;
            ViewBag.Canonical = "https://" + Request.Host + "/CoursePreview/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID; ;
            ViewBag.currentTab = "course";
            return View(vm);
        }
        [Route("CoursePreview/{title}/{id}",Order =1)]
        //[Route("CoursePreview",Order =2)]
        [HttpGet]
        public IActionResult CoursePreview(int ID, string Title, string utm_source, int Level = 1, int SessionID = 0)
        {
            ViewBag.currentTab = "CoursesList";
            HomeRepository Rep_Home = new HomeRepository();
            //if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            //{
            //    ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            //}
            if (utm_source != null)
            {
                CheckEarnLink(utm_source);
            }
            string NewTitle = Title.Replace("-", " ");
            var Course = _context.TblCourse.Where(a => a.ID == ID);

            Course.Include(a => a.TblUser).Load();
            Course.Include(a => a.TblGroup).Load();
            Course.Include(a => a.TblSession).ThenInclude(a => a.TblVideo).Load();
            Course.Include(a => a.TblUserLevels).Load();
            Course.Include(a => a.TblLevelPrice).Load();
            Course.Include(a => a.TblUserCourse).ThenInclude(a=>a.TblUser).Load();
            Course.Include(a => a.TblComment).ThenInclude(a => a.TblUserSender).Load();
            var qCourse = Course.SingleOrDefault();

            qCourse.Visit += 1;
            _context.Update(qCourse);
            _context.SaveChanges();
            if (qCourse == null)
            {
                return null;
            }
            string UserID = User.GetUserID();
            TimeUtility Time = new TimeUtility();
            var qFirstSession = qCourse.TblSession.Where(a => a.Level == Level).OrderBy(a => a.SessionSort)
                    .FirstOrDefault();
            if (qFirstSession != null)
            {
                if (SessionID <= 0)
                {
                    SessionID = qFirstSession.ID;
                }
                //if (Level <= 0)
                //{
                //    Level = qFirstSession.Level;
                //}
            }
            FileRepository RepImg = new FileRepository();
            CourseRepository RepCourse = new CourseRepository();
            VmCourses vm = new VmCourses();
            vm.TeacherUserName = qCourse.TblUser.UserName;
            vm.ID = qCourse.ID;
            vm.GroupName = qCourse.TblGroup.Title;
            vm.CIntroductory = qCourse.CIntroductory;
            vm.Link = "/CoursePreview/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID;
            vm.Teacher = qCourse.TblUser.FirstName + " " + qCourse.TblUser.LastName;
            vm.Title = qCourse.Title;
            vm.Text = qCourse.Text;
            vm.Description = qCourse.Description;
            vm.Date = qCourse.Date.ToShamsi().ToString("yyyy/MM/dd");
            vm.LastUpdate = Time.GetTimeName(qCourse.LastUpdate);
            vm.DiscountDescription = qCourse.DiscountDescription;
            vm.DownloadFileCount = qCourse.DownloadFileCount;
            vm.Prerequisites = qCourse.Prerequisites;
            vm.InSupport = qCourse.InSupport;
            vm.ProgressPercent = qCourse.ProgressPercent;
            vm.CommentCount = qCourse.TblComment.Where(a => a.Status == 1).Count();
            if (qCourse.ImageID != null)
            {
                var Image = RepImg.GetImageByID(qCourse.ImageID);
                vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
            }
            if (qCourse.ISMNCode != null)
            {
                vm.ISMNCode = qCourse.ISMNCode.ToString().ConvertNumerals();
            }
            var qBuyer = qCourse.TblUserCourse.Where(a => a.UserID == UserID).FirstOrDefault();
            if (qBuyer != null)
            {
                vm.UserBuyer = true;
            }
            ViewBag.TotalVideo = RepCourse.TotalVideo(qCourse.ID).ToString().ConvertNumerals();
            ViewBag.TotalSession = qCourse.TblSession.Count().ToString().ConvertNumerals();
            vm.CourseLevel = qCourse.CourseLevel;
            #region هنرجویان دوره
            vm.LstStudents = new List<CourseStudens>();
            ViewBag.CourseStudentsCount = qCourse.TblUserCourse.Count() -4 ;
            foreach (var item in qCourse.TblUserCourse.Take(4))
            {
                CourseStudens cs = new CourseStudens();
                cs.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                if (item.TblUser.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                    cs.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                vm.LstStudents.Add(cs);
            }
            #endregion

            vm.Placement = qCourse.Placement;
            var qChat = _context.TblMessages.Where(a => a.SenderID == UserID).Where(a => a.CourseID == qCourse.ID)
                .OrderByDescending(a => a.Date).FirstOrDefault();
            if (qChat != null && qChat.ChatID != null)
            {
                vm.ChatID = (int)qChat.ChatID;
            }

            //var TeacherImage = RepImg.GetImageByID(qCourse.TblUser.ImageID);
            //vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;
            vm.FullTime = RepCourse.GetCourseTime(qCourse.ID);
            int levelCount = qCourse.CIntroductory; /*+ qCourse.CMedium + qCourse.CAdvanced;*/

            vm.LstLevel = new List<Levels>();
            // نمایش قسمت محتوای دوره

            bool IsAdminRole = User.IsInRole("Admin");
            #region بررسی خرید کل دوره
            // ##### بررسی خرید کل دوره : 1000
            var qFullBuy = qCourse.TblUserCourse.Where(a => a.UserID == UserID).Where(a => a.LevelID == 1000).SingleOrDefault();
            var userLevelBuy = qCourse.TblUserLevels.Where(a => a.UserID == UserID).ToList();
            if (qFullBuy != null || userLevelBuy.Count()>= levelCount)
            {
                ViewBag.FullBuy = true;
            }
            //#####
            #endregion
            #region پرسش های متداول دوره
            RepCourse.GetCourseFaq(vm, _context, qCourse);
            #endregion
            #region بررسی خرید کل و قیمت
            // قیمت کل و تخفیف
            int UserLevelPrice = 0;
            #region امتیاز دهی

            vm.CourseRating = new CourseRating();
            var qRating = RepCourse.GetCourseRating(qCourse.ID);
            vm.CourseRating.Rating = qRating.Rating;
            vm.CourseRating.RatingCount = qRating.RatingCount;
            vm.CourseRating.FiveStar = qRating.FiveStar;
            vm.CourseRating.FourStar = qRating.FourStar;
            vm.CourseRating.ThreeStar = qRating.ThreeStar;
            vm.CourseRating.TwoStar = qRating.TwoStar;
            vm.CourseRating.OneStar = qRating.OneStar;

            #endregion
            if (qCourse.TblLevelPrice.Count() > 0)
            {
                int TotalPrice = qCourse.TblLevelPrice.Sum(a => a.Prise);
                vm.TotalPrice = TotalPrice.ToString("N0").ConvertNumerals();

                vm.CourseDiscount = new CourseDiscount();
                vm.CourseDiscount.Discount = 0;

                var qUserLevels = qCourse.TblUserLevels.Where(a => a.UserID == UserID);

                // کسر سطح های خریداری شده یک کاربر از قیمت کل
                if (qUserLevels.Count() > 0)
                {
                    foreach (var item in qUserLevels)
                    {
                        var qLevelPrice = qCourse.TblLevelPrice.Where(a => a.Level == item.Level).FirstOrDefault();
                        if (qLevelPrice != null)
                        {
                            UserLevelPrice = UserLevelPrice + qLevelPrice.Prise;
                        }
                    }
                    vm.Price = (TotalPrice - UserLevelPrice).ToString("N0").ConvertNumerals();
                }

                if (qCourse.DiscountPercent > 0)
                {
                    //محاسبه تخفیف بر اساس درصد
                    int BuyPrice = TotalPrice - UserLevelPrice;
                    int DiscountPrice = ((qCourse.DiscountPercent * (BuyPrice)) / 100).Value;
                    int Finalprice = (BuyPrice - DiscountPrice);

                    #region محاسبه سطح های خریداری شده برای تخفیف کل دوره
                    // اگر محدودیت سطح وجود نداشت
                    if (qCourse.DiscountRemaining == null || qCourse.DiscountRemaining == 0)
                    {
                        vm.CourseDiscount.RemainingCount = -1000;
                        vm.CourseDiscount.LevelsPurchased = 0;
                    }
                    // اگر محدودیت سطح وجود داشت
                    else
                    {
                        vm.CourseDiscount.LevelsPurchased = qUserLevels.Count();
                        vm.CourseDiscount.RemainingCount = (int)qCourse.DiscountRemaining - vm.CourseDiscount.LevelsPurchased;
                        if (vm.CourseDiscount.RemainingCount < 0)
                        {
                            // لغو تخفیف بدلیل محدودیت سطح
                            Finalprice = BuyPrice;
                        }
                    }
                    #endregion
                    vm.BasePrice = qCourse.TblLevelPrice.FirstOrDefault().Prise.ToString("N0").ConvertNumerals();
                    vm.Price = Finalprice.ToString("N0").ConvertNumerals();
                    vm.CourseDiscount.Discount = (int)qCourse.DiscountPercent;

                    if (Finalprice >= TotalPrice)
                    {
                        vm.Price = null;
                    }
                }
                ViewBag.price = qCourse.TblLevelPrice.FirstOrDefault().Prise * 10;
            }
            #endregion
            #region نمایش دیدگاه ها
            var qCourseComments = qCourse.TblComment.Where(a => a.Status == 1).OrderByDescending(a => a.Date);
            int Take = 10;
            int Page = 1;
            int Count = qCourseComments.Count();
            Page = Page > (int)Math.Ceiling((decimal)Count / Take) ? (int)Math.Ceiling((decimal)Count / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            vm.Session = new Session();
            vm.Session.LstComments = new List<SessionComments>();
            ViewBag.BlogReviewScore = _context.TblScore.Where(a => a.TitleEn == "CourseReview").SingleOrDefault().Value;
            foreach (var item in qCourseComments.Take(Take))
            {
                SessionComments sc = new SessionComments();
                sc.ID = item.ID;
                sc.Link = "/Profile/" + item.TblUserSender.UserName;
                sc.Text = item.Text;
                sc.Date = Time.GetDateName(item.Date).ToString().ConvertNumerals();
                sc.MiladiDate = item.Date.ToString("yyyy-MM-dd");
                if (item.BuyOffer != null)
                {
                    sc.BuyOffer = (int)item.BuyOffer;
                }
                sc.FullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                sc.UserName = item.TblUserSender.UserName;
                var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                if (item.UserSender == UserID)
                {
                    sc.IsMe = true;
                }
                if (item.ReplyID > 0)
                {
                    sc.ReplyID = (int)item.ReplyID;
                    var qReply = qCourse.TblComment.Where(a => a.ID == item.ReplyID).SingleOrDefault();
                    if (qReply != null)
                    {
                        sc.ReplyTo = qReply.TblUserSender.FirstName + " " + qReply.TblUserSender.LastName;
                    }
                }
                var qReview = _context.TblCourseReview.Where(a => a.CourseID == qCourse.ID).Where(a => a.UserID == item.UserSender).FirstOrDefault();
                if (qReview != null)
                {
                    sc.Rating = (decimal)qReview.Rating;
                }
                else if(item.ReplyID==null)
                {
                    TblCourseReview tr = new TblCourseReview();
                    tr.UserID = item.UserSender;
                    tr.CourseID = (int)item.CourseID;
                    tr.Rating = 5;
                    _context.Add(tr);
                    _context.SaveChanges();
                    sc.Rating = 5;
                }
                var qUserBuyer = qCourse.TblUserCourse.Where(a => a.UserID == item.UserSender).FirstOrDefault();
                if (qUserBuyer != null)
                {
                    sc.isBuyer = true;
                }

                vm.Session.LstComments.Add(sc);
            }

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)Count / Take);
            #endregion
            #region محتوای سطح
            RepCourse.GetCourseLevelContent(levelCount, qCourse, _context, vm, UserID, IsAdminRole, qFullBuy, _accessor);
            #region نمایش گفتگوها
            RepCourse.GetCourseConversion(vm, _context, UserID, qCourse, _accessor, Time, RepImg);
            #endregion

            #endregion
            ViewBag.MetaTag = qCourse.MetaTag;
            ViewBag.BKeywords = qCourse.Keywords;
            ViewBag.CatName = qCourse.Title;
            ViewBag.MetaDesc = qCourse.MetaDescription;
            ViewBag.mainBreadcrumb = "دوره آموزشی";
            ViewBag.BreadcrumbLink = "/CoursesList";
            ViewBag.Breadcrumb = qCourse.Title;
            ViewBag.Canonical = "https://" + Request.Host + "/CoursePreview/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID; ;
            ViewBag.currentTab = "course";
            return View(vm);
        }
        public IActionResult GetCourseComments(int CourseID, int Page=1)
        {
            var qCourse = _context.TblCourse.Where(a => a.ID == CourseID)
                            .Include(a => a.TblComment).ThenInclude(a => a.TblUserSender)
                            .Include(a => a.TblUserCourse).ThenInclude(a => a.TblUser)
                            .SingleOrDefault();

            FileRepository RepImg = new FileRepository();
            string UserID = User.GetUserID();
            TimeUtility Time = new TimeUtility();
            VmCourses vm = new VmCourses();
            vm.ID = qCourse.ID;
            vm.TeacherID = qCourse.TeacherID;

            #region نمایش دیدگاه ها
            var qCourseComments = qCourse.TblComment.Where(a => a.Status == 1).OrderByDescending(a => a.Date);

            int Take = 10;
            Page = Page <= 0 ? 1 : Page;
            int Count = qCourseComments.Count();
            Page = Page > (int)Math.Ceiling((decimal)Count / Take) ? (int)Math.Ceiling((decimal)Count / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            vm.Session = new Session();
            vm.Session.LstComments = new List<SessionComments>();
            ViewBag.BlogReviewScore = _context.TblScore.Where(a => a.TitleEn == "CourseReview").SingleOrDefault().Value;
            foreach (var item in qCourseComments.Skip(Skip).Take(Take))
            {
                SessionComments sc = new SessionComments();
                sc.ID = item.ID;
                sc.Link = "/Profile/" + item.TblUserSender.UserName;
                sc.Text = item.Text;
                sc.Date = Time.GetDateName(item.Date).ToString().ConvertNumerals();
                sc.MiladiDate = item.Date.ToString("yyyy-MM-dd");
                if (item.BuyOffer != null)
                {
                    sc.BuyOffer = (int)item.BuyOffer;
                }
                sc.FullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                sc.UserName = item.TblUserSender.UserName;
                var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                if (item.UserSender == UserID)
                {
                    sc.IsMe = true;
                }
                if (item.ReplyID > 0)
                {
                    sc.ReplyID = (int)item.ReplyID;
                    var qReply = qCourse.TblComment.Where(a => a.ID == item.ReplyID).SingleOrDefault();
                    if (qReply != null)
                    {
                        sc.ReplyTo = qReply.TblUserSender.FirstName + " " + qReply.TblUserSender.LastName;
                    }
                }
                var qReview = _context.TblCourseReview.Where(a => a.CourseID == qCourse.ID).Where(a => a.UserID == item.UserSender).FirstOrDefault();
                if (qReview != null)
                {
                    sc.Rating = (decimal)qReview.Rating;
                }
                var qUserBuyer = qCourse.TblUserCourse.Where(a => a.UserID == item.UserSender).FirstOrDefault();
                if (qUserBuyer != null)
                {
                    sc.isBuyer = true;
                }

                vm.Session.LstComments.Add(sc);
            }

            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)Count / Take);
            #endregion
            return PartialView("P_CourseComments", vm);
        }
        public IActionResult GetSessionContent(int ID, int SessionID, int Level)
        {
            var qCourse = _context.TblCourse.Where(a => a.ID == ID)
                .Include(a => a.TblUser)
                .Include(a => a.TblGroup)
                .Include(a => a.TblSession).ThenInclude(a => a.TblVideo)
                .Include(a => a.TblUserLevels)
                .Include(a => a.TblLevelPrice)
                .Include(a => a.TblComment).ThenInclude(a => a.TblUserSender)
                .SingleOrDefault();

            string UserID = User.GetUserID();
            bool IsAdminRole = User.IsInRole("Admin");
            FileRepository RepImg = new FileRepository();
            TimeUtility Time = new TimeUtility();
            VmCourses vm = new VmCourses();

            vm.TeacherUserName = qCourse.TblUser.UserName;
            vm.ID = qCourse.ID;
            vm.GroupName = qCourse.TblGroup.Title;
            vm.CIntroductory = qCourse.CIntroductory;
            //vm.CMedium = qCourse.CMedium;
            //vm.CAdvanced = qCourse.CAdvanced;
            vm.Teacher = qCourse.TblUser.FirstName + " " + qCourse.TblUser.LastName;
            vm.Title = qCourse.Title;
            vm.Date = qCourse.Date.ToShamsi().ToString("yyyy/MM/dd");
            vm.LastUpdate = Time.GetTimeName(qCourse.LastUpdate);

            var TeacherImage = RepImg.GetImageByID(qCourse.TblUser.ImageID);
            vm.Image = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;

            #region محتوای جلسه
            // نمایش محتوای جلسه
            string SecurityKey = "";
            if (SessionID > 0)
            {
                var qSession = _context.TblSession.Where(a => a.ID == SessionID).Where(a => a.Level == Level)
                    .Include(a => a.TblFiles)
                    .Include(a => a.TblVideo)
                    .Include(a => a.TblMessages)
                    .SingleOrDefault();

                var se = qCourse.TblSession.Where(a => a.Level == qSession.Level)
                    .OrderBy(a => a.SessionSort)
                    .ToList();

                vm.LstLevel = new List<Levels>();
                Levels lv = new Levels();
                lv.Number = qSession.Level;
                lv.LstSession = new List<Session>();
                int PrevSession = 0;
                foreach (var item in se)
                {
                    Session seItem = new Session();
                    seItem.ID = item.ID;

                    #region Next_And_Previous_Session
                    seItem.PreviousSessionID = PrevSession;
                    PrevSession = item.ID;
                    int nextSession = qCourse.TblSession.Where(a => a.ID == item.ID && a.Level == lv.Number).SingleOrDefault().SessionSort + 1;
                    var qNextSession = qCourse.TblSession.Where(a => a.SessionSort == nextSession).FirstOrDefault();
                    if (qNextSession != null)
                    {
                        seItem.NextSessionID = qNextSession.ID;
                    }
                    #endregion
                    lv.LstSession.Add(seItem);
                }
                vm.LstLevel.Add(lv);
                if (qSession == null)
                {
                    vm.Error = "notFound";
                    return View(vm);
                }
                vm.Session = new Session();
                vm.Session.ID = qSession.ID;
                vm.Session.Description = qSession.Description;
                vm.Session.FileDescription = qSession.FileDescription;
                vm.Session.Level = qSession.Level;

                vm.Session.Title = qSession.Title;
                vm.Session.IsFree = qSession.IsFree;

                // بررسی نمایش جلسه برای افراد خریدار و یا دارای مجوز
                var userBuy = qCourse.TblUserLevels.Where(a => a.Level == Level).Where(a => a.UserID == UserID).SingleOrDefault();
                var sessionBuy = _context.TblUserSession.Where(a => a.SessionID == qSession.ID).Where(a => a.UserID == UserID).FirstOrDefault();
                bool FreeSession = qSession.IsFree;
                if (userBuy != null || sessionBuy != null || IsAdminRole || UserID == qCourse.TeacherID || FreeSession)
                {
                    vm.Session.IsFree = true;

                    string IpAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    //string test = "185.120.195.138";

                    var SecurityIp = IpAddress.ConvertScurity();
                    string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("Armis" + "&" + SecurityIp));
                    SecurityKey = svcCredentials;
                }
                // End

                // نمایش ویدئو ها
                #region نمایش ویدئو ها
                FileRepository RepVideo = new FileRepository();
                vm.Session.LstVideo = new List<SessionVideos>();
                TimeSpan FullTime = new TimeSpan();
                string Number = "";

                //int PrevId = 0;
                foreach (var item in qSession.TblVideo.OrderBy(a => a.Sort))
                {
                    #region SetVideoTime
                    string Num = item.Number.Value.ToString();
                    if (!Number.Contains(Num) || Number == "")
                    {
                        if (item.Time != null && item.Time.Contains(":"))
                        {
                            string[] cTime = item.Time.Split(new string[] { ":" }, StringSplitOptions.None);
                            TimeSpan t1 = TimeSpan.Parse("00:" + cTime[0] + ":" + cTime[1]);

                            FullTime += t1;
                            Number += item.Number + ",";
                        }
                    }
                    #endregion
                    SessionVideos sv = new SessionVideos();
                    #region Next_And_Previous_Video
                    //sv.PreviousVideoID = PrevId;
                    //PrevId = item.ID;

                    //int nextVid = (int)qSession.TblVideo.Where(a => a.ID == item.ID).SingleOrDefault().Sort + 1;
                    //var qNextVideo = qSession.TblVideo.Where(a => a.Sort == nextVid).FirstOrDefault();
                    //if (qNextVideo != null)
                    //{
                    //    sv.NextVideoID = qNextVideo.ID;
                    //}
                    #endregion
                    if (item.ID == qSession.TblVideo.OrderBy(a => a.Sort).FirstOrDefault().ID)
                    {
                        vm.Session.FirstVideoID = item.ID;
                    }
                    sv.ID = item.ID;
                    sv.Alt = item.Alt;
                    sv.Title = item.Title;
                    sv.Quality = item.Quality;
                    sv.Number = (int)item.Number;
                    sv.Sort = (int)item.Sort;
                    sv.FileName = item.FileName;
                    //sv.Link = Video.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Video.TblServer.Path.Trim(new char[] { '/' }) + "/" + Video.FileName;
                    if (item.Url != null)
                    {
                        sv.dlLink = item.Url + "?u=" + SecurityKey;
                        //sv.Link2 = "/vid/" + item.ID + "/" + item.Token;
                    }
                    else
                    {
                        var Video = RepVideo.GetVideoByID(item.ID);
                        string Path = Video.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Video.TblServer.Path.Trim(new char[] { '/' }) + "/" + Video.FileName;
                        sv.dlLink = Path + "?u=" + SecurityKey;
                    }
                    vm.Session.LstVideo.Add(sv);
                }
                //vm.Session.FullTime = time.GetHourseTime(minute, second);
                vm.Session.FullTime = FullTime.ToString();
                #endregion
                #region نمایش فایل
                vm.Session.File = new SessionFile();
                if (qSession.TblFiles.Count > 0)
                {
                    var file = qSession.TblFiles.FirstOrDefault();
                    var Video = RepVideo.GetFileByID(file.ID);

                    vm.Session.File.Alt = file.Alt;
                    vm.Session.File.Description = file.Description;
                    vm.Session.File.FileName = file.FileName;
                    //vm.Session.File.Link = Video.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Video.TblServer.Path.Trim(new char[] { '/' }) + "/" + Video.FileName;
                    vm.Session.File.Link = "/dl/file/" + file.ID + "/" + file.Token;
                }
                #endregion
                // 
                #region نمایش دیدگاه ها
                vm.Session.LstComments = new List<SessionComments>();
                foreach (var item in qCourse.TblComment.Where(a => a.Status == 1))
                {
                    SessionComments sc = new SessionComments();
                    sc.ID = item.ID;
                    sc.Link = "/Profile/" + item.TblUserSender.UserName;
                    sc.Text = item.Text;
                    sc.Date = Time.GetDateName(item.Date);
                    sc.FullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                    var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                    sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    if (item.UserSender == UserID)
                    {
                        sc.IsMe = true;
                    }
                    vm.Session.LstComments.Add(sc);
                }
                #endregion
                // 
                #region نمایش گفتگوها
                vm.Session.LstConversion = new List<SessionConversion>();
                var qMessage = _context.TblMessages.Where(a => a.SessionID == qSession.ID)
                    .Include(a => a.TblUser)
                    .Where(a => a.SenderID == UserID || a.ReaseverID == UserID)
                    .Include(a => a.TblFiles)
                    .ToList();
                foreach (var item in qMessage)
                {
                    SessionConversion sc = new SessionConversion();
                    sc.ID = item.ID;
                    sc.Text = item.Text;
                    sc.Date = Time.GetDateName(item.Date);
                    sc.FullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                    var Image = RepImg.GetImageByID(item.TblUser.ImageID);
                    sc.ProfileImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    if (item.SenderID == UserID)
                    {
                        sc.IsMe = true;
                    }
                    if (item.TblFiles != null)
                    {
                        sc.File = new CMessageFile();
                        sc.File.ID = item.TblFiles.ID;
                        var file = RepImg.GetFileByID(item.TblFiles.ID);
                        //sc.File.Link = file.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + file.TblServer.Path.Trim(new char[] { '/' }) + "/" + file.FileName;
                        sc.File.Link = "/dl/file/" + item.TblFiles.ID + "/" + item.TblFiles.Token;
                        sc.File.Title = item.TblFiles.Title;
                    }
                    vm.Session.LstConversion.Add(sc);
                }
                #endregion
            }
            #endregion
            return PartialView("P_SessionContent", vm);
        }
        [HttpGet]
        [Route("dl/{type}/{id}/{token}")]
        public async Task<IActionResult> dl(string Type, int ID, string Token)
        {
            HomeRepository RepHome = new HomeRepository();
            FileRepository RepFile = new FileRepository();
            string path = "";
            if (Type == "vid")
            {
                bool access = RepHome.CheckVideoAccess(User.GetUserID(), ID, User.IsInRole("Admin"));
                if (!access)
                {
                    RedirectToAction("AccessDenied", "Account");
                }
                var qVideo = await _context.TblVideo.Where(a => a.ID == ID)
                .Where(a => a.Token == Token)
                 .Include(a => a.TblServer)
                 .SingleOrDefaultAsync();

                if (qVideo.ServerID != null && qVideo.ServerID > 0)
                {
                    path = qVideo.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + qVideo.TblServer.Path.Trim(new char[] { '/' }) + "/" + qVideo.FileName;
                }
                else
                {
                    path = qVideo.Url;
                }
            }
            if (Type == "file")
            {
                var qVideo = RepFile.GetFileByToken(ID, Token);
                path = qVideo.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + qVideo.TblServer.Path.Trim(new char[] { '/' }) + "/" + qVideo.FileName;
            }
            Stream stream = null;
            //This controls how many bytes to read at a time and send to the client
            int bytesToRead = 10000;

            // Buffer to read bytes in chunk size specified above
            byte[] buffer = new Byte[bytesToRead];

            string ext = Path.GetExtension(path);
            string fileName = Path.GetFileName(path) + ext;
            var contentType = FileContentType.GetMimeType(ext);
            // The number of bytes read
            try
            {
                //Create a WebRequest to get the file
                HttpWebRequest fileReq = (HttpWebRequest)HttpWebRequest.Create(path);

                //Create a response for this request
                HttpWebResponse fileResp = (HttpWebResponse)fileReq.GetResponse();

                if (fileReq.ContentLength > 0)
                    fileResp.ContentLength = fileReq.ContentLength;

                //Get the Stream returned from the response
                stream = fileResp.GetResponseStream();

                // prepare the response to the client. resp is the client Response
                var resp = HttpContext.Response;

                //Indicate the type of data being sent
                resp.ContentType = contentType;

                //Name the file 
                resp.Headers.Add("Accept-Ranges", "bytes");
                resp.Headers.Add("Content-Disposition", "attachment; filename=" + fileName);
                resp.Headers.Add("Content-Length", fileResp.ContentLength.ToString());

                int length;
                do
                {
                    // Verify that the client is connected.
                    if (!HttpContext.RequestAborted.IsCancellationRequested)
                    {
                        // Read data into the buffer.
                        length = stream.Read(buffer, 0, bytesToRead);

                        // and write it out to the response's output stream
                        resp.Body.Write(buffer, 0, length);


                        //Clear the buffer
                        buffer = new Byte[bytesToRead];
                    }
                    else
                    {
                        // cancel the download if client has disconnected
                        length = -1;
                    }
                }
                while (length > 0); //Repeat until no data is read
            }
            finally
            {
                if (stream != null)
                {
                    //Close the input stream
                    stream.Close();
                }
            }
            return File(stream, contentType, enableRangeProcessing: true);
        }

        [Route("vid/{id}/{token}")]
        public async Task vid(int ID, string Token)
        {
            string path = "";
            var qVideo = await _context.TblVideo.Where(a => a.ID == ID)
                .Where(a => a.Token == Token)
                 .Include(a => a.TblServer)
                 .SingleOrDefaultAsync();

            if (qVideo.ServerID != null && qVideo.ServerID > 0)
            {
                path = qVideo.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + qVideo.TblServer.Path.Trim(new char[] { '/' }) + "/" + qVideo.FileName;
            }
            else
            {
                path = qVideo.Url;
            }
            IPHostEntry heserver = Dns.GetHostEntry(Dns.GetHostName());
            var ip = heserver.AddressList[1].ToString();

            var SecurityIp = ip.ConvertScurity();
            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("Armis" + "&" + SecurityIp));

            var resp = HttpContext.Response;

            resp.Redirect(path);

        }

        [Route("Blog/{title?}")]
        [HttpGet]
        public IActionResult Blog(int Page = 1, string Title = "", string s = "")
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            List<VmBlog> LstBlog = new List<VmBlog>();
            FileRepository RepImg = new FileRepository();
            TimeUtility Time = new TimeUtility();

            if (string.IsNullOrEmpty(Title))
            {
                #region Blog Search And Blog List

                var qNews = _context.TblNews
                    .Where(a => a.Status == true)
                    .Where(a => a.ShortLink != null);

                qNews.Include(a => a.TblUser).Load();
                qNews.Include(a => a.TblImage).ThenInclude(a => a.TblServer).Load();

                if (!string.IsNullOrEmpty(s))
                {
                    qNews = qNews.Where(a => a.Title.Contains(s));
                    ViewBag.CatName = "جستجو";
                    ViewBag.Search = s;
                }
                int Take = 9;
                Page = Page <= 0 ? 1 : Page;
                int Count = qNews.Count();
                Page = Page > (int)Math.Ceiling((decimal)Count / Take) ? (int)Math.Ceiling((decimal)Count / Take) : Page;
                int Skip = (Take * Page) - Take; // (r * x) - r

                if (qNews.Count() > 0)
                {
                    qNews = qNews.OrderByDescending(a => a.Date).Skip(Skip).Take(Take);
                    foreach (var item in qNews)
                    {
                        VmBlog vm = new VmBlog();
                        vm.ID = item.ID;
                        vm.Title = item.Title;
                        vm.Pin = item.Pin;
                        vm.Date = Time.GetDateName(item.Date);
                        vm.UserFullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                        vm.UserName = item.TblUser.UserName;
                        //vm.Text = item.Text.Length > 150 ? item.Text.Substring(0, 150) + " ..." : item.Text;
                        vm.Visit = item.Visit;
                        vm.Link = "/Blog/" + item.ShortLink.Replace(" ", "-");
                        var Image = RepImg.GetImageByID(item.ImageID);
                        if (Image != null)
                        {
                            vm.ImageUrl = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                            string url = "wwwroot/images/thumbnails/" + item.TblImage.FileName;
                            if (System.IO.File.Exists(url))
                            {
                                vm.ImageUrl = "/images/thumbnails/" + item.TblImage.FileName;
                            }
                        }
                        else
                        {
                            vm.ImageUrl = "/images/armis-cover.jpg";
                        }
                        var UImage = RepImg.GetImageByID(item.TblUser.ImageID);
                        vm.UserImage = UImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + UImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + UImage.FileName;

                        var qReview = _context.TblBlogReview.Where(a => a.BlogID == item.ID);
                        if (qReview.Count() > 0)
                        {
                            decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                            vm.Rating = (decimal)Math.Round(Avrage, 1);
                            vm.RatingCount = qReview.Count();
                        }
                        LstBlog.Add(vm);
                    }
                }
                ViewBag.Take = Take;
                ViewBag.CurrentPage = Page;
                ViewBag.CountAllPage = (int)Math.Ceiling((decimal)Count / Take);
                ViewBag.Canonical = "https://" + Request.Host + "/Blog";
                #endregion Blog Search And Blog List
            }
            else if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Title))
            {
                #region Blog Details
                string NewTitle = Title.Replace("-", " ");
                var qNews = _context.TblNews.Where(a => a.ShortLink == NewTitle)
                           .Where(a => a.ShortLink != null)
                           .Where(a => a.Status == true);

                qNews.Include(a => a.TblNews_Cat).ThenInclude(a => a.TblCategory).Load();
                qNews.Include(a => a.TblNews_Key).ThenInclude(a => a.TblKeyword).Load();
                qNews.Include(a => a.TblUser).Load();
                qNews.Include(a => a.TblImage).ThenInclude(a => a.TblServer).Load();
                qNews.Include(a => a.TblNewsComments).ThenInclude(a => a.TblUser).Load();

                foreach (var item in qNews.ToList())
                {
                    VmBlog vm = new VmBlog();
                    vm.ID = item.ID;
                    vm.Title = item.Title;
                    vm.Date = Time.GetDateName(item.Date);
                    vm.OrginalDate = item.Date.ToString("yyyyy-MM-ddTHH:mm:ss");
                    vm.UserID = item.UserID;
                    vm.UserName = item.TblUser.UserName;
                    vm.UserFullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                    vm.UserDescription = item.TblUser.Description;
                    vm.Text = item.Text;
                    vm.Visit = item.Visit;
                    vm.Link = "/Blog/" + item.ShortLink.Replace(" ", "-");
                    vm.Description = item.Description;

                    var Image = RepImg.GetImageByID(item.ImageID);
                    var uImage = RepImg.GetImageByID(item.TblUser.ImageID);

                    if (uImage != null)
                    {
                        vm.UserImage = uImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + uImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + uImage.FileName;
                    }
                    if (Image != null)
                    {
                        vm.ImageUrl = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    }
                    else
                    {
                        vm.ImageUrl = "/images/armis-cover.jpg";
                    }
                    var IsAdminUser = User.IsInRole("Admin");
                    vm.LstComments = new List<BlogComment>();
                    foreach (var Citem in item.TblNewsComments.Where(a => a.Status == 1).OrderByDescending(a => a.Date))
                    {
                        BlogComment bc = new BlogComment();
                        bc.ID = Citem.ID;
                        bc.Text = Citem.Text;
                        bc.Date = Time.GetDateName(Citem.Date).ToString().ConvertNumerals();
                        if (Citem.ReplyID > 0)
                        {
                            bc.ReplyID = (int)Citem.ReplyID;
                            var qReply = item.TblNewsComments.Where(a => a.ID == bc.ReplyID).SingleOrDefault();
                            if (qReply != null)
                            {
                                if (qReply.TblUser != null)
                                {
                                    bc.ReplyTo = qReply.TblUser.FirstName + " " + qReply.TblUser.LastName;
                                }
                                else
                                {
                                    bc.ReplyTo = qReply.Name;
                                }
                            }
                        }
                        if (Citem.SenderID != null)
                        {
                            if (item.UserID == Citem.SenderID || IsAdminUser)
                            {
                                bc.IsAdmin = true;
                            }
                            if (!string.IsNullOrEmpty(Citem.TblUser.FirstName))
                            {
                                bc.SenderName = Citem.TblUser.FirstName + " " + Citem.TblUser.LastName;
                            }
                            else
                            {
                                bc.SenderName = Citem.TblUser.UserName;
                            }

                            var pImage = RepImg.GetImageByID(Citem.TblUser.ImageID);
                            bc.ProfileImage = pImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + pImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + pImage.FileName;
                        }
                        else
                        {
                            bc.SenderName = Citem.TblUser != null ? Citem.TblUser.FirstName + " " + Citem.TblUser.LastName : Citem.Name;
                            bc.ProfileImage = "/app-assets/app/media/img/users/100_1.png";
                        }
                        vm.LstComments.Add(bc);
                    }
                    vm.LstTags = new List<BlogTags>();
                    foreach (var tItem in item.TblNews_Key)
                    {
                        BlogTags bt = new BlogTags();
                        bt.ID = tItem.ID;
                        bt.Title = tItem.TblKeyword.Title;
                        bt.Link = "/Tags/" + tItem.TblKeyword.Title.Replace(" ", "-");
                        vm.LstTags.Add(bt);
                    }
                    vm.LstCategory = new List<BlogCategory>();
                    foreach (var tItem in item.TblNews_Cat)
                    {
                        BlogCategory bc = new BlogCategory();
                        bc.ID = tItem.ID;
                        bc.Title = tItem.TblCategory.Title;
                        bc.Link = "/Category/" + tItem.TblCategory.TitleEn.Replace(" ", "-");
                        vm.LstCategory.Add(bc);
                    }
                    HomeRepository RepHome = new HomeRepository();
                    RepHome.AddVisit(item.ID);
                    ViewBag.CatName = item.Title;

                    var qReview = _context.TblBlogReview.Where(a => a.BlogID == item.ID);
                    if (qReview.Count() > 0)
                    {
                        decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                        vm.Rating = (decimal)Math.Round(Avrage, 1);
                        vm.RatingCount = qReview.Count();
                    }
                    LstBlog.Add(vm);
                    ViewData["AdsList"] = Rep_Home.GetBlogAds(item.AdsList);

                }
                ViewBag.BlogBanner = Rep_Home.GetSingleBanner("blog-banner");
                ViewBag.BlogDetails = "true";
                ViewBag.MetaDesc = qNews.FirstOrDefault().MetaDescription;
                ViewBag.BKeywords = qNews.FirstOrDefault().Keywords;
                ViewBag.Canonical = "https://" + Request.Host + "/Blog/" + qNews.FirstOrDefault().ShortLink.Replace(" ", "-");
                #endregion Blog Details
            }
            ViewBag.currentTab = "Blog";
            ViewBag.mainBreadcrumb = "وبلاگ";
            // get reCAPTHCA key from appsettings.json
            ViewData["ReCaptchaKey"] = _configuration.GetSection("GoogleReCaptcha:key").Value;
            ViewData["Cats"] = _context.TblCategory.Include(a => a.TblNews_Cat).ToList();
            return View(LstBlog);
        }
        [Route("Blog3/{title?}")]
        [HttpGet]
        public IActionResult Blog3(int Page = 1, string Title = "", string s = "")
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            List<VmBlog> LstBlog = new List<VmBlog>();
            FileRepository RepImg = new FileRepository();
            TimeUtility Time = new TimeUtility();

            if (string.IsNullOrEmpty(Title))
            {
                #region Blog Search And Blog List

                var qNews = _context.TblNews
                    .Where(a => a.Status == true)
                    .Where(a => a.ShortLink != null);

                qNews.Include(a => a.TblUser).Load();
                qNews.Include(a => a.TblImage).ThenInclude(a => a.TblServer).Load();

                if (!string.IsNullOrEmpty(s))
                {
                    qNews = qNews.Where(a => a.Title.Contains(s));
                    ViewBag.CatName = "جستجو";
                    ViewBag.Search = s;
                }
                int Take = 9;
                Page = Page <= 0 ? 1 : Page;
                int Count = qNews.Count();
                Page = Page > (int)Math.Ceiling((decimal)Count / Take) ? (int)Math.Ceiling((decimal)Count / Take) : Page;
                int Skip = (Take * Page) - Take; // (r * x) - r

                if (qNews.Count() > 0)
                {
                    qNews = qNews.OrderByDescending(a => a.Date).Skip(Skip).Take(Take);
                    foreach (var item in qNews)
                    {
                        VmBlog vm = new VmBlog();
                        vm.ID = item.ID;
                        vm.Title = item.Title;
                        vm.Pin = item.Pin;
                        vm.Date = Time.GetDateName(item.Date);
                        vm.UserFullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                        vm.UserName = item.TblUser.UserName;
                        //vm.Text = item.Text.Length > 150 ? item.Text.Substring(0, 150) + " ..." : item.Text;
                        vm.Visit = item.Visit;
                        vm.Link = "/Blog/" + item.ShortLink.Replace(" ", "-");
                        var Image = RepImg.GetImageByID(item.ImageID);
                        if (Image != null)
                        {
                            vm.ImageUrl = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                            string url = "wwwroot/images/thumbnails/" + item.TblImage.FileName;
                            if (System.IO.File.Exists(url))
                            {
                                vm.ImageUrl = "/images/thumbnails/" + item.TblImage.FileName;
                            }
                        }
                        else
                        {
                            vm.ImageUrl = "/images/armis-cover.jpg";
                        }
                        var UImage = RepImg.GetImageByID(item.TblUser.ImageID);
                        vm.UserImage = UImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + UImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + UImage.FileName;

                        var qReview = _context.TblBlogReview.Where(a => a.BlogID == item.ID);
                        if (qReview.Count() > 0)
                        {
                            decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                            vm.Rating = (decimal)Math.Round(Avrage, 1);
                            vm.RatingCount = qReview.Count();
                        }
                        LstBlog.Add(vm);
                    }
                }
                ViewBag.Take = Take;
                ViewBag.CurrentPage = Page;
                ViewBag.CountAllPage = (int)Math.Ceiling((decimal)Count / Take);
                ViewBag.Canonical = "https://" + Request.Host + "/Blog";
                #endregion Blog Search And Blog List
            }
            else if (!string.IsNullOrEmpty(Title) && !string.IsNullOrEmpty(Title))
            {
                #region Blog Details
                string NewTitle = Title.Replace("-", " ");
                var qNews = _context.TblNews.Where(a => a.ShortLink == NewTitle)
                           .Where(a => a.ShortLink != null)
                           .Where(a => a.Status == true);

                qNews.Include(a => a.TblNews_Cat).ThenInclude(a => a.TblCategory).Load();
                qNews.Include(a => a.TblNews_Key).ThenInclude(a => a.TblKeyword).Load();
                qNews.Include(a => a.TblUser).Load();
                qNews.Include(a => a.TblImage).ThenInclude(a => a.TblServer).Load();
                qNews.Include(a => a.TblNewsComments).ThenInclude(a => a.TblUser).Load();

                foreach (var item in qNews.ToList())
                {
                    VmBlog vm = new VmBlog();
                    vm.ID = item.ID;
                    vm.Title = item.Title;
                    vm.Date = Time.GetDateName(item.Date);
                    vm.OrginalDate = item.Date.ToString("yyyyy-MM-ddTHH:mm:ss");
                    vm.UserID = item.UserID;
                    vm.UserName = item.TblUser.UserName;
                    vm.UserFullName = item.TblUser.FirstName + " " + item.TblUser.LastName;
                    vm.UserDescription = item.TblUser.Description;
                    vm.Text = item.Text;
                    vm.Visit = item.Visit;
                    vm.Link = "/Blog/" + item.ShortLink.Replace(" ", "-");
                    vm.Description = item.Description;

                    var Image = RepImg.GetImageByID(item.ImageID);
                    var uImage = RepImg.GetImageByID(item.TblUser.ImageID);

                    if (uImage != null)
                    {
                        vm.UserImage = uImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + uImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + uImage.FileName;
                    }
                    if (Image != null)
                    {
                        vm.ImageUrl = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    }
                    else
                    {
                        vm.ImageUrl = "/images/armis-cover.jpg";
                    }
                    var IsAdminUser = User.IsInRole("Admin");
                    vm.LstComments = new List<BlogComment>();
                    foreach (var Citem in item.TblNewsComments.Where(a => a.Status == 1).OrderByDescending(a => a.Date))
                    {
                        BlogComment bc = new BlogComment();
                        bc.ID = Citem.ID;
                        bc.Text = Citem.Text;
                        bc.Date = Time.GetDateName(Citem.Date).ToString().ConvertNumerals();
                        if (Citem.ReplyID > 0)
                        {
                            bc.ReplyID = (int)Citem.ReplyID;
                            var qReply = item.TblNewsComments.Where(a => a.ID == bc.ReplyID).SingleOrDefault();
                            if (qReply != null)
                            {
                                if (qReply.TblUser != null)
                                {
                                    bc.ReplyTo = qReply.TblUser.FirstName + " " + qReply.TblUser.LastName;
                                }
                                else
                                {
                                    bc.ReplyTo = qReply.Name;
                                }
                            }
                        }
                        if (Citem.SenderID != null)
                        {
                            if (item.UserID == Citem.SenderID || IsAdminUser)
                            {
                                bc.IsAdmin = true;
                            }
                            if (!string.IsNullOrEmpty(Citem.TblUser.FirstName))
                            {
                                bc.SenderName = Citem.TblUser.FirstName + " " + Citem.TblUser.LastName;
                            }
                            else
                            {
                                bc.SenderName = Citem.TblUser.UserName;
                            }

                            var pImage = RepImg.GetImageByID(Citem.TblUser.ImageID);
                            bc.ProfileImage = pImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + pImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + pImage.FileName;
                        }
                        else
                        {
                            bc.SenderName = Citem.TblUser != null ? Citem.TblUser.FirstName + " " + Citem.TblUser.LastName : Citem.Name;
                            bc.ProfileImage = "/app-assets/app/media/img/users/100_1.png";
                        }
                        vm.LstComments.Add(bc);
                    }
                    vm.LstTags = new List<BlogTags>();
                    foreach (var tItem in item.TblNews_Key)
                    {
                        BlogTags bt = new BlogTags();
                        bt.ID = tItem.ID;
                        bt.Title = tItem.TblKeyword.Title;
                        bt.Link = "/Tags/" + tItem.TblKeyword.Title.Replace(" ", "-");
                        vm.LstTags.Add(bt);
                    }
                    vm.LstCategory = new List<BlogCategory>();
                    foreach (var tItem in item.TblNews_Cat)
                    {
                        BlogCategory bc = new BlogCategory();
                        bc.ID = tItem.ID;
                        bc.Title = tItem.TblCategory.Title;
                        bc.Link = "/Category/" + tItem.TblCategory.TitleEn.Replace(" ", "-");
                        vm.LstCategory.Add(bc);
                    }
                    HomeRepository RepHome = new HomeRepository();
                    RepHome.AddVisit(item.ID);
                    ViewBag.CatName = item.Title;

                    var qReview = _context.TblBlogReview.Where(a => a.BlogID == item.ID);
                    if (qReview.Count() > 0)
                    {
                        decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                        vm.Rating = (decimal)Math.Round(Avrage, 1);
                        vm.RatingCount = qReview.Count();
                    }
                    LstBlog.Add(vm);
                    ViewData["AdsList"] = Rep_Home.GetBlogAds(item.AdsList);

                }
                ViewBag.BlogBanner = Rep_Home.GetSingleBanner("blog-banner");
                ViewBag.BlogDetails = "true";
                ViewBag.MetaDesc = qNews.FirstOrDefault().MetaDescription;
                ViewBag.BKeywords = qNews.FirstOrDefault().Keywords;
                ViewBag.Canonical = "https://" + Request.Host + "/Blog/" + qNews.FirstOrDefault().ShortLink.Replace(" ", "-");
                #endregion Blog Details
            }
            ViewBag.currentTab = "Blog";
            ViewBag.mainBreadcrumb = "وبلاگ";
            // get reCAPTHCA key from appsettings.json
            ViewData["ReCaptchaKey"] = _configuration.GetSection("GoogleReCaptcha:key").Value;
            ViewData["Cats"] = _context.TblCategory.Include(a => a.TblNews_Cat).ToList();
            return View(LstBlog);
        }

        private readonly CancellationTokenSource _cts = new CancellationTokenSource();

        [Route("Tags/{title?}")]
        public IActionResult Tags(string Title, int Page = 1)
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            List<VmBlog> LstBlog = new List<VmBlog>();
            FileRepository RepImg = new FileRepository();
            TimeUtility Time = new TimeUtility();

            #region Blog Tags
            string newTitle = Title.Replace("-", " ");
            var qTags = _context.TblNews_Key.Where(a => a.TblKeyword.Title == newTitle)
                .Where(a => a.TblNews.Status == true)
                .Include(a => a.TblKeyword)
                .Include(a => a.TblNews).ThenInclude(a => a.TblImage).ThenInclude(a => a.TblServer)
                .Include(a => a.TblNews).ThenInclude(a => a.TblUser).AsEnumerable();

            if (qTags.Count() <= 0)
            {
                qTags = qTags.ToList();
            }

            int Take = 9;
            Page = Page <= 0 ? 1 : Page;
            int Count = qTags.Count();
            Page = Page > (int)Math.Ceiling((decimal)Count / Take) ? (int)Math.Ceiling((decimal)Count / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            foreach (var item in qTags.Skip(Skip).Take(Take))
            {
                VmBlog vm = new VmBlog();
                vm.ID = item.TblNews.ID;
                vm.Title = item.TblNews.Title;
                vm.Date = Time.GetDateName(item.TblNews.Date);
                vm.UserName = item.TblNews.TblUser.FirstName + " " + item.TblNews.TblUser.LastName;
                vm.Text = item.TblNews.Text;
                vm.Visit = item.TblNews.Visit;
                vm.Link = "/Blog/" + item.TblNews.ShortLink.Replace(" ", "-");
                var Image = RepImg.GetImageByID(item.TblNews.ImageID);
                if (Image != null)
                {
                    vm.ImageUrl = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    string url = "wwwroot/images/thumbnails/" + item.TblNews.TblImage.FileName;
                    if (System.IO.File.Exists(url))
                    {
                        vm.ImageUrl = "/images/thumbnails/" + item.TblNews.TblImage.FileName;
                    }
                }
                else
                {
                    vm.ImageUrl = "/images/armis-cover.jpg";
                }

                var UImage = RepImg.GetImageByID(item.TblNews.TblUser.ImageID);
                vm.UserImage = UImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + UImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + UImage.FileName;
                var qReview = _context.TblBlogReview.Where(a => a.BlogID == item.TblNews.ID);
                if (qReview.Count() > 0)
                {
                    decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                    vm.Rating = (decimal)Math.Round(Avrage, 1);
                    vm.RatingCount = qReview.Count();
                }
                LstBlog.Add(vm);
            }

            ViewBag.CatName = newTitle;
            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)Count / Take);
            #endregion Blog Tags

            ViewBag.currentTab = "Blog";
            ViewBag.mainBreadcrumb = "برچسب";
            ViewBag.Breadcrumb = ViewBag.CatName;
            ViewBag.Canonical = "https://" + Request.Host + "/Blog";
            ViewData["Cats"] = _context.TblCategory.Include(a => a.TblNews_Cat).ToList();
            return View(LstBlog);
        }
        [Route("Category/{Title}")]
        public IActionResult Category(string Title, int Page = 1)
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            List<VmBlog> LstBlog = new List<VmBlog>();
            FileRepository RepImg = new FileRepository();
            TimeUtility Time = new TimeUtility();

            #region Blog Category
            // نمایش دسته بندی
            string newTitle = Title.Replace("-", " ");
            var qCat = _context.TblCategory.Where(a => a.TitleEn == newTitle);
            if (qCat.Count() <= 0)
            {
                ViewData["Cats"] = _context.TblCategory.Include(a => a.TblNews_Cat).ToList();
                return View(LstBlog);
            }
            qCat.Include(a => a.TblNews_Cat).ThenInclude(a => a.TblNews).ThenInclude(a => a.TblUser).Load();
            qCat.Include(a => a.TblNews_Cat).ThenInclude(a => a.TblNews).ThenInclude(a => a.TblImage).ThenInclude(a => a.TblServer).Load();

            var category = qCat.FirstOrDefault().TblNews_Cat.Where(a => a.TblNews.Status == true).OrderByDescending(a => a.TblNews.Date).AsQueryable();

            int Take = 9;
            Page = Page <= 0 ? 1 : Page;
            int Count = category.Count();
            Page = Page > (int)Math.Ceiling((decimal)Count / Take) ? (int)Math.Ceiling((decimal)Count / Take) : Page;
            int Skip = (Take * Page) - Take; // (r * x) - r

            foreach (var item in category.Skip(Skip).Take(Take))
            {
                VmBlog vm = new VmBlog();
                vm.ID = item.TblNews.ID;
                vm.Title = item.TblNews.Title;
                vm.Date = Time.GetDateName(item.TblNews.Date);
                vm.UserName = item.TblNews.TblUser.FirstName + " " + item.TblNews.TblUser.LastName;
                vm.Text = item.TblNews.Text;
                vm.Visit = item.TblNews.Visit;
                vm.Link = "/Blog/" + item.TblNews.ShortLink.Replace(" ", "-");
                var Image = RepImg.GetImageByID(item.TblNews.ImageID);
                if (Image != null)
                {
                    vm.ImageUrl = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    string url = "wwwroot/images/thumbnails/" + item.TblNews.TblImage.FileName;
                    if (System.IO.File.Exists(url))
                    {
                        vm.ImageUrl = "/images/thumbnails/" + item.TblNews.TblImage.FileName;
                    }
                }
                else
                {
                    vm.ImageUrl = "/images/armis-cover.jpg";
                }

                var UImage = RepImg.GetImageByID(item.TblNews.TblUser.ImageID);
                vm.UserImage = UImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + UImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + UImage.FileName;

                var qReview = _context.TblBlogReview.Where(a => a.BlogID == item.TblNews.ID);
                if (qReview.Count() > 0)
                {
                    decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                    vm.Rating = (decimal)Math.Round(Avrage, 1);
                    vm.RatingCount = qReview.Count();
                }
                LstBlog.Add(vm);
            }

            ViewBag.CatName = _context.TblCategory.Where(a => a.TitleEn == newTitle).FirstOrDefault().Title;
            ViewBag.Take = Take;
            ViewBag.CurrentPage = Page;
            ViewBag.CountAllPage = (int)Math.Ceiling((decimal)Count / Take);
            #endregion Blog Category

            ViewBag.currentTab = "Blog";
            ViewBag.Canonical = "https://" + Request.Host + "/Blog";
            ViewData["Cats"] = _context.TblCategory.Include(a => a.TblNews_Cat).ToList();
            ViewBag.MetaDesc = qCat.FirstOrDefault().MetaDescription;
            ViewBag.mainBreadcrumb = "دسته بندی";
            ViewBag.Breadcrumb = ViewBag.CatName;
            return View(LstBlog);
        }

        public IActionResult OnlineLearn()
        {
            ViewData["mainBreadcrumb"] = "آموزش آنلاین";
            return View();
        }
        [Route("ClassView/{title}/{id}", Order = 1)]
        public IActionResult ClassView(int ID, string Title)
        {
            string NewTitle = Title.Replace("-", " ");
            var OnlineCourse = _context.TblOnlineCourse.Where(a => a.ID == ID)
                .Include(a=>a.TblOnlineScheduling).Include(a=>a.TblOnlineCoursePrice);

            OnlineCourse.Include(a=>a.TblUser).Load();
            OnlineCourse.Include(a => a.TblOnlineGroup).Load();

            var qOnlineCourse = OnlineCourse.SingleOrDefault();
            VmOnlineCourse vm = new VmOnlineCourse();
            TimeUtility Time = new TimeUtility();
            PayRepository RepPay = new PayRepository();
            FileRepository RepImg = new FileRepository();

            vm.ID = qOnlineCourse.ID;
            vm.Title = qOnlineCourse.Title;
            vm.Type = qOnlineCourse.Type;
            vm.Teacher = qOnlineCourse.TblUser.FirstName + " " + qOnlineCourse.TblUser.LastName;
            vm.TeacherUserName = qOnlineCourse.TblUser.UserName;
            vm.Status = qOnlineCourse.Status;
            vm.Date = Time.GetTimeName(qOnlineCourse.Date);
            vm.Visit = qOnlineCourse.Visit;
            vm.SessionsCount = qOnlineCourse.SessionsCount;
            vm.Capacity = qOnlineCourse.Capacity;
            vm.Description = qOnlineCourse.Description;
            vm.Link = "/ClassView/" + (qOnlineCourse.ShortLink != null ? qOnlineCourse.ShortLink.Replace(" ", "-") : qOnlineCourse.Title.Replace(" ", "-")) + "/" + qOnlineCourse.ID;
            
            var groupID = qOnlineCourse.TblOnlineGroup.GroupID;
            var subGroup = _context.TblOnlineGroup.Where(a => a.ID == groupID).FirstOrDefault();
            if (subGroup != null)
            {
                vm.GroupName = subGroup.Title;
                vm.SubGroupName = qOnlineCourse.TblOnlineGroup.Title;
            }
            else
            {
                vm.GroupName = qOnlineCourse.TblOnlineGroup.Title;
                vm.SubGroupName = qOnlineCourse.TblOnlineGroup.Title;
            }
            if (qOnlineCourse.ImageID != null)
            {
                var Image = RepImg.GetImageByID(qOnlineCourse.ImageID);
                vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
            }
            else
            {
                vm.Image = "/assets/media/misc/image2.png";
            }
            var TeacherImage = RepImg.GetImageByID(qOnlineCourse.TblUser.ImageID);
            vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;

            if (qOnlineCourse.Type == 1)
            {
                vm.TotalPrice = qOnlineCourse.TotalPrice;
                if (qOnlineCourse.StartDate.Year != 0001)
                {
                    vm.StartDate = qOnlineCourse.StartDate.ToShamsi().ToString("yyyy/MM/dd").ConvertNumerals();
                }
                else
                {
                    vm.StartDate = "تعیین نشده";
                }
                if (qOnlineCourse.EndDate.Year != 0001)
                {
                    vm.EndDate = qOnlineCourse.EndDate.ToShamsi().ToString("yyyy/MM/dd").ConvertNumerals();
                }
                else
                {
                    vm.EndDate = "تعیین نشده";
                }
                vm.LstScheduling = new List<VmCourseScheduling>();
                foreach (var item in qOnlineCourse.TblOnlineScheduling)
                {
                    VmCourseScheduling vs = new VmCourseScheduling();
                    vs.Title = item.Title;
                    vs.Sort = item.Sort;
                    vs.StartDate = item.StartDate.ToShamsi().ToString("yyyy/MM/dd");
                    vs.EndDate = item.EndDate.ToShamsi().ToString("yyyy/MM/dd");
                    vm.LstScheduling.Add(vs);
                }
            }
            else if (qOnlineCourse.Type == 2)
            {
                if (qOnlineCourse.TblOnlineCoursePrice != null)
                {
                    vm.TotalPrice = qOnlineCourse.TblOnlineCoursePrice.Price;
                }
                RepPay.GetPackageViewPrice(qOnlineCourse.TblOnlineCoursePrice, vm);
            }
            ViewBag.mainBreadcrumb = "آموزش آنلاین";
            return View(vm);
        }
        public IActionResult ClassPackages(int ID, int pkg = 0)
        {
            var qClass = _context.TblOnlineCourse.Where(a => a.ID == ID).Include(a => a.TblOnlineCoursePrice).SingleOrDefault();
            VmOnlineCourse vm = new VmOnlineCourse();
            vm.ID = qClass.ID;
            if (qClass.TblOnlineCoursePrice != null)
            {
                vm.PackagesPrice = new VmCoursePackagesPrice();
                vm.PackagesPrice.PkgPrice1 = qClass.TblOnlineCoursePrice.Price;
                vm.PackagesPrice.PkgPrice2 = qClass.TblOnlineCoursePrice.Price * 2;
                vm.PackagesPrice.PkgPrice3 = qClass.TblOnlineCoursePrice.Price * 4;
                vm.PackagesPrice.PkgPrice4 = qClass.TblOnlineCoursePrice.Price * 8;
                vm.PackagesPrice.PkgPrice5 = qClass.TblOnlineCoursePrice.Price * 12;
                vm.PackagesPrice.PkgDiscount1 = vm.PackagesPrice.PkgPrice1 - qClass.TblOnlineCoursePrice.PkgDiscount1;
                vm.PackagesPrice.PkgDiscount2 = vm.PackagesPrice.PkgPrice2 - qClass.TblOnlineCoursePrice.PkgDiscount2;
                vm.PackagesPrice.PkgDiscount3 = vm.PackagesPrice.PkgPrice3 - qClass.TblOnlineCoursePrice.PkgDiscount3;
                vm.PackagesPrice.PkgDiscount4 = vm.PackagesPrice.PkgPrice4 - qClass.TblOnlineCoursePrice.PkgDiscount4;
                vm.PackagesPrice.PkgDiscount5 = vm.PackagesPrice.PkgPrice5 - qClass.TblOnlineCoursePrice.PkgDiscount5;
            }
            ViewBag.mainBreadcrumb = "آموزش آنلاین";
            ViewBag.breadcrumb = "انتخاب پکیج";
            return View(vm);
        }

        public IActionResult ClassBooking(int ID,int pkg=0)
        {
            if(pkg == 1 || pkg ==2 || pkg == 4 || pkg == 8 || pkg == 12)
            {
                var qClass = _context.TblOnlineCourse.Where(a => a.ID == ID)
                    .Include(a=>a.TblOnlineCoursePrice)
                    .Include(a=>a.TblUser).SingleOrDefault();
                if (qClass == null)
                {
                    return RedirectToAction(nameof(Index));
                }
                VmOnlineCourse vm = new VmOnlineCourse();
                FileRepository RepImg = new FileRepository();
                PayRepository RepPay = new PayRepository();

                var TeacherImage = RepImg.GetImageByID(qClass.TblUser.ImageID);
                vm.ID = ID;
                vm.Title = qClass.Title;
                vm.Teacher = qClass.TblUser.FirstName + " " + qClass.TblUser.LastName;
                vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;
                vm.GroupName = RepPay.GetPackageName(pkg);
                if (User.Identity.IsAuthenticated)
                {
                    ViewBag.userID = User.GetUserID();
                }
                if (qClass.Type == 2)
                {
                    vm.PackagesPrice = new VmCoursePackagesPrice();
                    vm.PackagesPrice.PkgPrice1 = qClass.TblOnlineCoursePrice.Price;
                    vm.DiscountPrice = RepPay.GetPackageDiscountPrice(pkg, qClass.TblOnlineCoursePrice);
                    vm.TotalPrice = RepPay.GetPackagePrice(pkg,qClass.TblOnlineCoursePrice);
                    vm.FinalPrice= RepPay.GetPackageFinalPrice(pkg, qClass.TblOnlineCoursePrice);
                    //vm.ProfitPrice= RepPay.GetProfitPackagePrice(pkg, vm.TotalPrice, qClass.TblOnlineCoursePrice);
                }
                ViewBag.mainBreadcrumb = "آموزش آنلاین";
                ViewBag.breadcrumb = "رزرو کلاس";
                return View(vm);
            }
            return RedirectToAction(nameof(Index));
        }
        [HttpGet]
        public JsonResult getEventData(int courseID)
        {
            var qEvents = _context.TblOnlineScheduling.Where(a => a.CourseID == courseID);
            var qUserEvents = _context.TblUserEvents.Where(a => a.CourseID == courseID)
                .Where(a=>a.Enable==true).ToList();

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
            foreach (var item in qUserEvents)
            {
                VmScheduleEvent vm = new VmScheduleEvent();
                vm.id = item.ID.ToString();
                vm.start = item.StartDate;
                vm.end = item.EndDate;
                vm.title = "رزرو شده";
                vm.overlap = false;
                vm.groupId = "reservedForClass";
                //vm.display = "background";
                vm.color = "grey";
                vm.constraint = "reservedForClass";
                LstVm.Add(vm);
            }
            return Json(LstVm.ToArray());
        }

        [Route("ClassList/{id}", Order = 1)]
        public IActionResult ClassList(string ID)
        {
            int Type = 0;
            string section = "کلاس های آنلاین";
            if (ID == "Private")
            {
                Type = 2;
                section = "کلاس های خصوصی";
            }
            var qCourse = _context.TblOnlineCourse.Where(a => a.Status == 1).AsQueryable();

            qCourse.Include(t => t.TblOnlineGroup).Load();
            qCourse.Include(t => t.TblUser).Load();
            if (Type > 0)
            {
                qCourse = qCourse.Where(a => a.Type == Type).OrderByDescending(a => a.Date);
            }
            List<VmOnlineCourse> lstCourse = new List<VmOnlineCourse>();
            CourseRepository Rep_Course = new CourseRepository();
            FileRepository RepImg = new FileRepository();

            var DataList = Rep_Course.GetOnlineCourseListData(qCourse);
            lstCourse.AddRange(DataList);

            ViewData["mainBreadcrumb"] = "آموزش آنلاین";
            ViewData["breadcrumb"] = section;
            return View(lstCourse);
        }
        [HttpPost]
        public async Task<IActionResult> SendBlogComment(TblNewsComments t)
        {
            var qNews = _context.TblNews.Where(a => a.ID == t.NewsID)
                .Include(a => a.TblUser)
                .SingleOrDefault();
            string Email = "";
            string smsNumber = "";

            if (string.IsNullOrEmpty(t.Text))
            {
                TempData["Message"] = "خطا ! لطفا دیدگاه خود را وارد نمایید";
                TempData["Style"] = "alert-danger";
                return RedirectToAction(nameof(Blog), new { Title = qNews.ShortLink.Replace(" ", "-") });
            }
            if (User.Identity.IsAuthenticated)
            {
                t.SenderID = User.GetUserID();
                if (User.IsInRole("Admin") || t.SenderID == qNews.UserID)
                {
                    t.Status = 1;
                }
            }
            else if (!ReCaptchaPassed(
            Request.Form["g-recaptcha-response"], // that's how you get it from the Request object
            _configuration.GetSection("GoogleReCaptcha:secret").Value,
            _logger))
            {
                TempData["Message"] = "خطا ! لطفا گزینه 'من ربات نیستم' را تایید نمایید";
                TempData["Style"] = "alert-danger";
                return RedirectToAction(nameof(Blog), new { Title = qNews.ShortLink.Replace(" ", "-") });
            }
            ToolsRepository Rep_Tools = new ToolsRepository();
            if (t.ReplyID > 0)
            {
                // اگر پاسخ به دیدگاه باشد
                var qReplyComment = _context.TblNewsComments.Where(a => a.ID == t.ReplyID)
                    .Include(a => a.TblUser).SingleOrDefault();
                t.ReaseverID = qReplyComment.SenderID;
                if (t.ReaseverID == qNews.UserID)
                {
                    // اگر گیرنده نویسنده بود
                    Email = Rep_Tools.Settings().Email;
                    smsNumber = Rep_Tools.Settings().Phone;
                }
                else
                {
                    // اگر گیرنده کاربر بود
                    Email = qReplyComment.TblUser != null ? qReplyComment.TblUser.Email : qReplyComment.Email;
                    smsNumber = qReplyComment.TblUser != null ? qReplyComment.TblUser.Mobile : "";
                }
            }
            else
            {
                // گیرنده نویسنده است
                t.ReaseverID = Rep_Tools.Settings().Email;
                Email = Rep_Tools.Settings().Email;
                smsNumber = Rep_Tools.Settings().Phone;
            }
            t.Date = DateTime.Now;
            _context.Add(t);
            _context.SaveChanges();
            #region SendNotif

            string link = "https://" + Request.Host + "/Blog/" + qNews.ShortLink.Replace(" ", "-");
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
                    text = "کاربر گرامی به دیدگاه شما در صفحه " + qNews.Title + " پاسخ داده شد. ";

                    string Body = EmailTemplate.Replace("[TITLE]", title).Replace("[TEXT]", text)
                    .Replace("[LINK]", link).Replace("[LINK-TITLE]", "مشاهده");
                    await _emailSender.SendEmailNoticesAsync(Email, "دریافت دیدگاه جدید", Body);
                }
                else if (t.Status != 1)
                {
                    // اعلان دیدگاه به نویسنده پست
                    title = "ارسال نظر برای پست شما";
                    text = qNews.TblUser.FirstName + " " + qNews.TblUser.LastName + " گرامی برای پست شما در وبلاگ آکادمی آرمیس نظری ارسال شده است";

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
                    smsText = "هنرجوی عزیز به دیدگاه شما پاسخ داده شد.برای مشاهده روی لینک کلیک کنید" + " " + Environment.NewLine + " " + link;
                }
                else if (t.Status != 1)
                {
                    smsText = qNews.TblUser.FirstName + " " + qNews.TblUser.LastName + "گرامی برای پست شما در وبلاگ آکادمی آرمیس نظری ارسال شده است برای مشاهده روی لینک کلیک نمایید"
                        + " " + Environment.NewLine + " " + link;
                }
                string mCodeSender = sms.SendSms(smsText, smsNumber);
            }
            #endregion
            TempData["Message"] = "دیدگاه شما با موفقیت ارسال گردید و پس از تایید نمایش داده خواهد شد";
            TempData["Style"] = "alert-success";
            return RedirectToAction(nameof(Blog), new { Title = qNews.ShortLink.Replace(" ", "-") });
        }
        [HttpPost]
        public async Task<IActionResult> CommentReply(TblNewsComments t)
        {
            var qComment = await _context.TblNewsComments.Where(a => a.ID == t.ReplyID)
                .Include(a => a.TblNews).Include(a => a.TblUser)
                .SingleOrDefaultAsync();
            string Email = "";
            if (qComment.SenderID != null)
            {
                t.ReaseverID = qComment.SenderID;
                if (qComment.TblUser.Email != null)
                {
                    Email = qComment.Email;
                }
            }
            else
            {
                t.ReaseverID = qComment.TblNews.UserID;
                Email = t.Email;
            }
            if (User.Identity.IsAuthenticated)
            {
                t.SenderID = User.GetUserID();
            }
            #region SendEmail
            if (!string.IsNullOrEmpty(Email))
            {
                string EmailTemplate = "";
                using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                {
                    EmailTemplate = reader.ReadToEnd();
                }
                //string text = "جهت مشاهده دیدگاه خود بر روی لینک زیر کلیک نمایید";
                string link = qComment.TblNews.ShortLink.Replace(" ", "-");
                string Body = EmailTemplate.Replace("[TITLE]", "دریافت دیدگاه جدید").Replace("[TEXT]", t.Text)
                    .Replace("[LINK]", link).Replace("[LINK-TITLE]", "مشاهده");
                await _emailSender.SendEmailNoticesAsync(Email, "دریافت دیدگاه جدید", Body);
            }
            #endregion

            t.NewsID = qComment.NewsID;
            t.Date = DateTime.Now;
            await _context.AddAsync(t);
            await _context.SaveChangesAsync();

            TempData["Message"] = "دیدگاه شما با موفقیت ارسال گردید و پس از تایید نمایش داده خواهد شد";
            TempData["Style"] = "alert-success";
            return RedirectToAction(nameof(Blog), new { Title = qComment.TblNews.ShortLink.Replace(" ", "-") });
        }
        public static bool ReCaptchaPassed(string gRecaptchaResponse, string secret, ILogger logger)
        {
            HttpClient httpClient = new HttpClient();
            var res = httpClient.GetAsync($"https://www.google.com/recaptcha/api/siteverify?secret={secret}&response={gRecaptchaResponse}").Result;
            if (res.StatusCode != HttpStatusCode.OK)
            {
                logger.LogError("Error while sending request to ReCaptcha");
                return false;
            }

            string JSONres = res.Content.ReadAsStringAsync().Result;
            dynamic JSONdata = JObject.Parse(JSONres);
            if (JSONdata.success != "true")
            {
                return false;
            }

            return true;
        }
        public IActionResult Faqs()
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            var qFaqs = _context.TblGuide.Where(a => a.Type == 4).ToList();

            ViewBag.currentTab = "Faqs";
            ViewBag.Canonical = "https://" + Request.Host + "/Faqs";
            ViewBag.mainBreadcrumb = "پرسش های متداول";
            return View(qFaqs);
        }
        public IActionResult TermsConditions()
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            var qFaqs = _context.TblGuide.Where(a => a.Type == 5).ToList();

            ViewBag.currentTab = "Home";
            return View(qFaqs);
        }
        public IActionResult Contact()
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            ViewBag.currentTab = "contact";
            ViewBag.Canonical = "https://" + Request.Host + "/Contact";
            ViewData["mainBreadcrumb"] = "تماس با ما";
            return View();
        }
        [HttpPost]
        public IActionResult Contact(TblContact t)
        {
            try
            {
                if (!User.Identity.IsAuthenticated)
                {
                    if (!ReCaptchaPassed(
                Request.Form["g-recaptcha-response"], // that's how you get it from the Request object
                _configuration.GetSection("GoogleReCaptcha:secret").Value,
                _logger))
                    {
                        TempData["Message"] = "خطا ! لطفا گزینه 'من ربات نیستم' را تایید نمایید";
                        TempData["Style"] = "alert-danger";
                        return RedirectToAction(nameof(Contact));
                    }
                    if (string.IsNullOrEmpty(t.Name) || string.IsNullOrEmpty(t.Email) || string.IsNullOrEmpty(t.Mobile))
                    {
                        TempData["Message"] = "خطا ! لطفا تمام فیلد ها را پر نمایید";
                        TempData["Style"] = "alert-danger";
                        return RedirectToAction(nameof(Contact));
                    }
                }
                else
                {
                    t.UserID = User.GetUserID();
                }
                t.Date = DateTime.Now;
                t.Type = 0;
                _context.Add(t);
                _context.SaveChanges();

                TempData["Message"] = "درخواست شما با موفقیت ارسال گردید. به زودی به آن رسیدگی و پاسخ آن از طریق راه های ارتباطی  فرستاده می شود.";
                TempData["Style"] = "alert-success";
            }
            catch (Exception)
            {
                TempData["Message"] = "خطا در عملیات . لطفا مجددا در زمان دیگر تلاش نمایید";
                TempData["Style"] = "alert-danger";
                throw;
            }
            return RedirectToAction(nameof(Contact));
        }

        [HttpPost]
        public async Task<JsonResult> LandingForm(TblContact t, int Age)
        {
            try
            {
                switch (Age)
                {
                    case 1:
                        t.Description = "سن بین 7 تا 12 سال";
                        break;
                    case 2:
                        t.Description = "سن بین 13 تا 25 سال";
                        break;
                    case 3:
                        t.Description = "سن بین 26 تا 40 سال";
                        break;
                    case 4:
                        t.Description = "سن بین 41 تا 60 سال";
                        break;
                }
                t.Date = DateTime.Now;
                t.Type = 10;
                _context.Add(t);
                _context.SaveChanges();

                #region SendEmail
                ToolsRepository Rep_tools = new ToolsRepository();
                string text = "یک فرم جدید توسط کابر تکمیل شد ";

                string EmailTemplate = "";
                using (StreamReader reader = System.IO.File.OpenText(Path.Combine(_appEnvironment.ContentRootPath, "wwwroot/Email2.html")))
                {
                    EmailTemplate = reader.ReadToEnd();
                }
                string Body = EmailTemplate.Replace("[TITLE]", t.Title).Replace("[TEXT]", text)
                .Replace("[LINK]", "https://armisacademy.com/Admin/ContactManage").Replace("[LINK-TITLE]", "مشاهده");
                await _emailSender.SendEmailNoticesAsync(Rep_tools.Settings().Email, "ثبت فرم پیش ثبت نام", Body);
                #endregion

                return Json(new { result = "ok", msg = "اطلاعات با موفقیت ثبت گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "error", msg = "متاسفانه خطایی رخ داد لطفا با مدیریت در ارتباط باشید" });
            }
        }
        [HttpPost]
        public async Task<JsonResult> ReportLink(int ID)
        {
            try
            {
                var qVideo = await _context.TblVideo.Where(a => a.ID == ID).SingleAsync();
                var qSession = await _context.TblSession.Where(a => a.ID == qVideo.SessionID)
                    .Include(a => a.TblCourse)
                    .SingleAsync();

                TblContact t = new TblContact();
                t.Email = "";
                t.Mobile = "";
                if (User.Identity.IsAuthenticated)
                {
                    var qUser = User.GetUserDetails();
                    t.UserID = qUser.Id;
                    t.Name = qUser.FirstName + " " + qUser.LastName;
                    t.Email = qUser.Email;
                    t.Mobile = qUser.Mobile;
                }
                else
                {
                    t.Name = "کاربر مهمان";
                }
                t.Title = "گزارش خرابی لینک دانلود";
                t.Date = DateTime.Now;
                t.Type = 5;
                t.Description = "گزارش خرابی لینک دانلود در دوره " + " ' " + qSession.TblCourse.Title + " ' " + " سطح " + qSession.Level
                    + " در جلسه  " + " ' " + qSession.Title + " ' " + " با عنوان ویدئوی " + " ' " + qVideo.Title + " ' ";
                await _context.AddAsync(t);
                await _context.SaveChangesAsync();
                return Json(new { result = "ok", msg = "گزارش شما با موفقیت ثبت گردید" });
            }
            catch (Exception)
            {
                return Json(new { result = "faill", msg = "" });

            }
        }
        public IActionResult BecomingTeacher()
        {
            if (User.Identity.IsAuthenticated)
            {
                var qTicket = _context.TblTicket.Where(a => a.UserID == User.GetUserID()).Where(a => a.Section == 6).FirstOrDefault();
                if (qTicket != null)
                {
                    ViewBag.Submited = true;
                }
            }
            ViewData["mainBreadcrumb"] = "جذب استاد";
            return View();
        }
        public IActionResult BecomingTeacher3()
        {
            if (User.Identity.IsAuthenticated)
            {
                var qTicket = _context.TblTicket.Where(a => a.UserID == User.GetUserID()).Where(a => a.Section == 6).FirstOrDefault();
                if (qTicket != null)
                {
                    ViewBag.Submited = true;
                }
            }
            ViewData["mainBreadcrumb"] = "جذب استاد";
            return View();
        }
        [HttpGet]
        public IActionResult Cooperation()
        {
            ViewData["mainBreadcrumb"] = "همکاری با ما";
            return View();
        }
        public IActionResult Cooperation3()
        {
            ViewData["mainBreadcrumb"] = "همکاری با ما";
            return View();
        }

        [HttpPost]
        public JsonResult Cooperat(TblContact t)
        {
            try
            {
                if (t.Type == null)
                {
                    return Json(new { result = "faill", msg = "لطفا موضوع قرارداد را انتخاب نمایید" });
                }
                if (t.Name == null || t.Mobile == null)
                {
                    return Json(new { result = "faill", msg = "لطفا تمامی فیلد ها را تکمیل نمایید" });
                }
                if (!IsPhoneNumber(t.Mobile))
                {
                    return Json(new { result = "faill", msg = "تلفن وارد شده نامعتبر می باشد" });
                }
                TblContact tc = new TblContact();
                tc.Name = t.Name;
                tc.Description = t.Description;
                tc.Company = t.Company;
                tc.Email = t.Email;
                tc.Mobile = t.Mobile;
                tc.Type = t.Type;
                tc.Date = DateTime.Now;
                tc.Read = false;
                tc.Title = "همکاری با برند";
                _context.Add(tc);
                _context.SaveChanges();

                return Json(new { result = "ok", msg = "فرم شما با موفقیت ثبت گردید و بزودی کارشناسان ما با شما در تماس خواهند بود" });
            }
            catch (Exception e)
            {
                var text = e.InnerException;
                return Json(new { result = "faill", msg = "خطا در ارسال !" });

            }
        }
        public static bool IsPhoneNumber(string number)
        {
            return Regex.Match(number, @"(\+98|0)?9\d{9}").Success;
        }
        [HttpGet]
        public IActionResult Subscribe()
        {
            ViewBag.Success = false;
            return View();
        }
        [HttpPost]
        public IActionResult Subscribe(TblSubscription t)
        {
            try
            {
                var qSubs = _context.TblSubscription.Where(a => a.Email == t.Email).SingleOrDefault();
                if (!IsValidEmail(t.Email))
                {
                    TempData["Message"] = "ایمیل وارد شده صحیح نمی باشد";
                    TempData["Alert"] = "alert-danger";
                    ViewBag.Success = false;
                }
                if (qSubs != null)
                {
                    if (!qSubs.Status)
                    {
                        TempData["Message"] = "عضویت شما با موفقیت در خبرنامه وبسایت به ثبت رسید";
                        TempData["Alert"] = "alert-success";
                        ViewBag.Success = true;

                        qSubs.Status = true;
                        qSubs.Date = DateTime.Now;
                        _context.Update(qSubs);
                        _context.SaveChanges();
                    }
                    else
                    {
                        TempData["Message"] = "ایمیل شما قبلا در سیستم خبرنامه ما به ثبت رسیده است";
                        TempData["Alert"] = "alert-danger";
                        ViewBag.Success = false;
                    }
                }
                else
                {
                    t.Status = true;
                    t.Date = DateTime.Now;
                    _context.Add(t);
                    _context.SaveChanges();
                    TempData["Message"] = "عضویت شما با موفقیت در خبرنامه وبسایت به ثبت رسید";
                    TempData["Alert"] = "alert-success";

                    ViewBag.Success = true;
                }
            }
            catch (Exception)
            {
                ViewBag.Success = false;
                TempData["Message"] = "خطایی رخ داد !";
                TempData["Alert"] = "alert-danger";
            }
            return View();
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
        public IActionResult About()
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            ToolsRepository RepTools = new ToolsRepository();
            var setting = RepTools.Settings();
            ViewBag.currentTab = "about";
            ViewBag.Canonical = "https://" + Request.Host + "/About";
            ViewData["mainBreadcrumb"] = "درباره ما";
            return View(setting);
        }
        public IActionResult About3()
        {
            HomeRepository Rep_Home = new HomeRepository();
            if (Rep_Home.GetSingleBanner("top-header").ShowAllPage)
            {
                ViewBag.TopBanner = Rep_Home.GetSingleBanner("top-header");
            }
            ToolsRepository RepTools = new ToolsRepository();
            var setting = RepTools.Settings();
            ViewBag.currentTab = "about";
            ViewBag.Canonical = "https://" + Request.Host + "/About";
            ViewData["mainBreadcrumb"] = "درباره ما";
            return View(setting);
        }
        public IActionResult EarningSystem()
        {
            ViewBag.currentTab = "Earning";
            ViewBag.mainBreadcrumb = "کسب درآمد";
            return View();
        }
        public IActionResult ELink(int ID)
        {
            string path = "";
            var qLink = _context.TblUserEarnLinks.Where(a => a.ID == ID).SingleOrDefault();
            if (qLink != null)
            {
                //read cookie from IHttpContextAccessor  
                //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];

                //read cookie from Request object  
                string cookieValue = Request.Cookies["e_link"];
                if (cookieValue == null || Convert.ToInt32(cookieValue) != qLink.ID)
                {
                    SetCookie("e_link", qLink.ID.ToString(), 60);
                    qLink.Visit++;
                    _context.Update(qLink);
                    _context.SaveChanges();
                }
                if (qLink.UtmCampaign != null && qLink.UtmMedium != null && qLink.UtmSource != null)
                {
                    path= qLink.SiteLink+ "?utm_source="+ qLink.UtmSource + "&utm_medium="+ qLink.UtmMedium + "&utm_campaign=" + qLink.UtmCampaign;
                }
                else
                {
                    path = qLink.SiteLink + "?utm_source=ArmisUser-" + ID + "&utm_medium=banner&utm_campaign=course";
                }
            }
            else
            {
                path= "https://" + Request.Host;
            }
            return RedirectTo(path);
        }
        [HttpGet]
        public IActionResult RedirectTo(string url)
        {
            if (url == null)
            {
                return RedirectToAction(nameof(Index));
            }
            return Redirect(url);
        }
        public IActionResult Er404()
        {
            ViewBag.BKeywords = "خطای 404";
            ViewBag.MetaDesc = "خطای 404 - متاسفانه صفحه مورد نظر یافت نشد";
            return View();
        }
        [Route("Notic/{type}/{id}")]
        public IActionResult Notic(int ID, string Type = "")
        {
            switch (Type)
            {
                case "follow":
                    var qFollow = _context.TblFriends.Where(a => a.ID == ID).Include(a => a.TblUser).SingleOrDefault();

                    if (!qFollow.Read)
                    {
                        qFollow.Read = true;
                        _context.Update(qFollow);
                        _context.SaveChanges();
                    }
                    return LocalRedirect("/Profile/" + qFollow.TblUser.UserName);
            }
            return RedirectToAction("Index", "Home");

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
        public JsonResult SendRating(int ID, decimal Value)
        {
            var qReview = _context.TblBlogReview.Where(a => a.BlogID == ID);
            decimal UserRate = Math.Round(Value, 1);
            string UserID = null;
            int RCount = qReview.Count();
            if (User.Identity.IsAuthenticated)
            {
                UserID = User.GetUserID();
                var UserRating = qReview.Where(a => a.UserID == UserID).Where(a=>a.BlogID==ID).FirstOrDefault();
                if (UserRating != null)
                {
                    UserRating.Rating = UserRate;
                    _context.Update(UserRating);
                }
                else
                {
                    TblBlogReview t = new TblBlogReview();
                    t.UserID = UserID;
                    t.BlogID = ID;
                    t.Rating = UserRate;
                    _context.Add(t);

                    var qUser = _context.Users.Where(a => a.Id == UserID).SingleOrDefault();
                    var qScore = _context.TblScore.Where(a => a.TitleEn == "BlogReview").SingleOrDefault();
                    TblUserScore ts = new TblUserScore();
                    ts.UserID = UserID;
                    ts.ScoreID = qScore.ID;
                    _context.Add(ts);

                    qUser.Score += qScore.Value;
                    _context.Update(qUser);
                    RCount += 1;
                }
                //return Json(new { msg = "برای امتیاز دهی لطفا وارد حساب کاربری خود شوید" });
            }
            else
            {
                string SessionKey = "_blog_rating_" + ID;
                string GetSesionName = HttpContext.Session.GetString(SessionKey);
                if (GetSesionName != null)
                {
                    //HttpContext.Session.Remove(SessionKey);
                    //HttpContext.Session.SetString(SessionKey, UserRate.ToString());
                    return Json(new { msg = " شما قبلا به این پست امتیاز داده اید . تغییر امتیاز فقط برای کاربران سایت امکان پذیر می باشد" });
                }
                else
                {
                    HttpContext.Session.SetString(SessionKey, UserRate.ToString());

                    TblBlogReview t = new TblBlogReview();
                    t.BlogID = ID;
                    t.Rating = UserRate;
                    _context.Add(t);
                    RCount += 1;
                }
            }
            _context.SaveChanges();
            return Json(new { result = "ok", value = RCount, msg = "امتیاز ثبت گردید" });
        }
        public JsonResult SendCourseRating(int ID, decimal Value)
        {
            if (!User.Identity.IsAuthenticated)
            {
                return Json(new { msg = "برای امتیاز دهی لطفا وارد حساب کاربری خود شوید" });
            }
            string UserID = User.GetUserID();

            var qUserCourse = _context.TblUserCourse.Where(a => a.CourseID == ID).Where(a => a.UserID == UserID)
                .FirstOrDefault();
            if (qUserCourse == null)
            {
                return Json(new { msg = "برای امتیاز دهی باید دوره را خریداری کرده باشید" });
            }
            var qReview = _context.TblCourseReview.Where(a => a.CourseID == ID);

            int RCount = qReview.Count();
            var UserRating = qReview.Where(a => a.UserID == UserID).Where(a=>a.CourseID==ID).FirstOrDefault();
            if (UserRating != null)
            {
                UserRating.Rating = Value;
                _context.Update(UserRating);
            }
            else
            {
                TblCourseReview t = new TblCourseReview();
                t.UserID = UserID;
                t.CourseID = ID;
                t.Rating = Value;
                _context.Add(t);

                //var qUser = _context.Users.Where(a => a.Id == UserID).SingleOrDefault();

                //var qScore = _context.TblScore.Where(a => a.TitleEn == "BlogReview").SingleOrDefault();
                //TblUserScore ts = new TblUserScore();
                //ts.UserID = UserID;
                //ts.ScoreID = qScore.ID;
                //_context.Add(ts);
                //qUser.Score += qScore.Value;
                //_context.Update(qUser);

                RCount += 1;
            }
            _context.SaveChanges();
            return Json(new { result = "ok", value = RCount.ToString().ConvertNumerals(), msg = "امتیاز ثبت گردید" });
        }

        [HttpPost]
        public async Task<IActionResult> GetSessionDec(int ID)
        {
            var qSession = await _context.TblSession.Where(a => a.ID == ID).SingleOrDefaultAsync();
            return Json(new { text = qSession.Description });
        }
        public void CheckEarnLink(string Source)
        {
            var qLink = _context.TblUserEarnLinks.Where(a => a.UtmSource == Source).FirstOrDefault();
            if (qLink != null)
            {
                //read cookie from IHttpContextAccessor  
                //string cookieValueFromContext = _httpContextAccessor.HttpContext.Request.Cookies["key"];

                //read cookie from Request object  
                string cookieValue = Request.Cookies["e_link"];
                if (cookieValue == null || Convert.ToInt32(cookieValue) != qLink.ID)
                {
                    SetCookie("e_link", qLink.ID.ToString(), 60);
                    qLink.Visit++;
                    _context.Update(qLink);
                    _context.SaveChanges();
                }
            }
        }
        /// <summary>  
        /// set the cookie  
        /// </summary>  
        /// <param name="key">key (unique indentifier)</param>  
        /// <param name="value">value to store in cookie object</param>  
        /// <param name="expireTime">expiration time</param>  
        public void SetCookie(string key, string value, int? expireTime)
        {
            CookieOptions option = new CookieOptions();

            if (expireTime.HasValue)
                option.Expires = DateTime.Now.AddDays(expireTime.Value);
            else
                option.Expires = DateTime.Now.AddMilliseconds(10);

            Response.Cookies.Append(key, value, option);
        }
        /// <summary>  
        /// Delete the key  
        /// </summary>  
        /// <param name="key">Key</param>  
        public void RemoveCookie(string key)
        {
            Response.Cookies.Delete(key);
        }
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}