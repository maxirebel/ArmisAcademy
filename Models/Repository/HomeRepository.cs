using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.ExMethod;
using ArmisApp.Models.Utility;
using ArmisApp.Models.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace ArmisApp.Models.Repository
{
    public class HomeRepository : IDisposable
    {
        private DataContext db = null;

        public HomeRepository()
        {
            db = new DataContext();
        }
        public int UserCount()
        {
            var q = db.Users.ToList().Count();
            return q;
        }
        public List<VmNews> GetLastBlog(int Count,string Type)
        {
            var qNews = db.TblNews.Where(a => a.ShortLink != null).Where(a => a.Status == true).OrderByDescending(a => a.Date)
                .Take(Count)
                .AsQueryable();

            qNews.Include(a => a.TblUser).Load();
            qNews.Include(a => a.TblImage).Load();
            //qNews.Include(a => a.TblNews_Cat).ThenInclude(a => a.TblCategory).Load();

            if (Type == "news")
            {
                qNews = qNews.Where(a => a.Type == 1);
            }
            if (Type == "blog")
            {
                qNews = qNews.Where(a => a.Type == 0);
            }
            List<VmNews> lstNews = new List<VmNews>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qNews.ToList())
            {
                VmNews vm = new VmNews();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Visit = item.Visit;
                vm.Writer= item.TblUser.FirstName + " " + item.TblUser.LastName;
                //if (item.TblNews_Cat.Count > 0)
                //{
                //    vm.Categgory = item.TblNews_Cat.FirstOrDefault().TblCategory.Title;
                //}
                vm.Link = "/Blog/" + item.ShortLink.Replace(" ", "-");
                vm.Date = Time.GetDateName(item.Date);
                vm.Text = item.Text.Length > 120 ? item.Text.Substring(0, 120) + " ..." : item.Text;
                var ImageBlog = RepImg.GetImageByID(item.ImageID);
                if (ImageBlog!=null)
                {
                    vm.ImageUrl = ImageBlog.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + ImageBlog.TblServer.Path.Trim(new char[] { '/' }) + "/" + ImageBlog.FileName;
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
                var WriterImage = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.WriterImage = WriterImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + WriterImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + WriterImage.FileName;

                var qReview = db.TblBlogReview.Where(a => a.BlogID == item.ID);
                if (qReview.Count() > 0)
                {
                    decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                    vm.Rating = (decimal)Math.Round(Avrage, 1);
                }
                lstNews.Add(vm);
            }
            return lstNews;
        }
        public List<VmNews> GetRelatedPosts(int PostID)
        {
            var qTag = db.TblNews_Key.Where(a => a.NewsID == PostID).ToList();

            List<VmNews> lstNews = new List<VmNews>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();

            List<int> se = new List<int>();
            foreach (var itemCat in qTag)
            {
                var qNews = db.TblNews_Key
               .Where(a => a.TblNews.Status == true).Where(a => a.KeywordID == itemCat.KeywordID)
               .Where(a=>a.NewsID!= PostID)
               .Include(a => a.TblNews).ThenInclude(a => a.TblImage)
               .Include(a => a.TblNews).ThenInclude(a => a.TblUser).Take(10);
                foreach (var item in qNews)
                {
                    if (se.Any(a => a == item.TblNews.ID))
                    {
                        break;
                    }
                    VmNews vm = new VmNews();
                    vm.ID = item.TblNews.ID;
                    vm.Title = item.TblNews.Title;
                    vm.Link = "/Blog/" + item.TblNews.ShortLink.Replace(" ", "-");
                    vm.Date = Time.GetDateName(item.TblNews.Date);
                    vm.DayOfDate = Time.GetDayName(item.TblNews.Date);
                    vm.MonthOfDate = Time.GetMonthName(item.TblNews.Date);
                    vm.Text = item.TblNews.Text.Length > 120 ? item.TblNews.Text.Substring(0, 120) + " ..." : item.TblNews.Text;
                    vm.Writer = item.TblNews.TblUser.FirstName + " " + item.TblNews.TblUser.LastName;
                    var uImage = RepImg.GetImageByID(item.TblNews.TblUser.ImageID);
                    if (uImage != null)
                    {
                        vm.WriterImage = uImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + uImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + uImage.FileName;
                    }
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
                    lstNews.Add(vm);
                    se.Add(item.TblNews.ID);
                }
            }
            return lstNews.OrderBy(r => Guid.NewGuid()).Take(5).ToList();
        }
        public List<VmNews> GetPupulerNews(int Count)
        {
            var qNews = db.TblNews
                .Where(a => a.Status == true)
                .Include(a => a.TblUser)
                .Include(a => a.TblNews_Cat).ThenInclude(a => a.TblCategory)
                .Include(a=>a.TblImage)
                .OrderByDescending(a => a.Visit).Take(Count).ToList();

            List<VmNews> lstNews = new List<VmNews>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qNews)
            {
                VmNews vm = new VmNews();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Visit = item.Visit;
                vm.Writer = item.TblUser.FirstName + " " + item.TblUser.LastName;
                if (item.TblNews_Cat.Count > 0)
                {
                    vm.Categgory = item.TblNews_Cat.FirstOrDefault().TblCategory.Title;
                }
                vm.Link = "/Blog/" + item.ShortLink.Replace(" ","-");
                vm.Date = Time.GetDateName(item.Date);
                vm.Text = item.Text;
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
                lstNews.Add(vm);
            }
            return lstNews;
        }
        public List<VmCourses> GetLastCourse(int Count)
        {
            var qCourse = db.TblCourse.Take(Count);
            //.Include(a => a.TblGroup)
            //.Include(a => a.TblUserCourse)
            //.Include(a => a.TblLevelPrice)
            //.OrderByDescending(a => a.Date);.Take(Count).AsNoTracking().ToList();

            qCourse.Include(a => a.TblGroup).Load();
            qCourse.Include(a => a.TblUser).Load();
            qCourse.Include(a => a.TblLevelPrice).Load();

            List<VmCourses> lstCourse = new List<VmCourses>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            CourseRepository RepCourse = new CourseRepository();

            foreach (var item in qCourse)
            {
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.GroupName = item.TblGroup.Title;
                vm.Link = "/Course/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.ShortLink = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.Date = Time.GetTimeName(item.Date);
                vm.LastUpdate = Time.GetTimeName(item.LastUpdate);
                vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.FullTime= RepCourse.GetCourseTime(item.ID);

                if (item.TblLevelPrice.Count()>0)
                {
                    vm.TotalPrice = item.TblLevelPrice.FirstOrDefault().Prise.ToString("N0").ConvertNumerals() + " تومان";
                }
                else
                {
                    vm.TotalPrice = "تعیین نشده";
                }
                var Image = RepImg.GetImageByID(item.ImageID);
                vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;

                var TeacherImage = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;

                var qReview = db.TblCourseReview.Where(a => a.CourseID == item.ID);
                if (qReview.Count() > 0)
                {
                    decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                    vm.Rating = (decimal)Math.Round(Avrage, 1);
                    vm.RatingCount = qReview.Count();
                }
                lstCourse.Add(vm);
            }
            return lstCourse.OrderByDescending(a=>a.Date).ToList();
        }
        public List<VmCourseReview> GetBestReviews()
        {
            var qReviews = db.TblComment.Where(a => a.Status == 1).Where(a => a.TblCourse.TblCourseReview != null)
                .OrderByDescending(a => a.Date).Take(5).AsQueryable();

            qReviews.Include(a => a.TblCourse).ThenInclude(a => a.TblCourseReview).Load();
            qReviews.Include(a => a.TblUserSender).Load();

            var qReview = db.TblCourseReview.AsQueryable();
            List<VmCourseReview> LstVm = new List<VmCourseReview>();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qReviews)
            {
                VmCourseReview vm = new VmCourseReview();
                vm.UserFullName = item.TblUserSender.FirstName + " " + item.TblUserSender.LastName;
                vm.Text = item.Text.Length > 180 ? item.Text.Substring(0, 180) + " ..." : item.Text;
                vm.CourseTitle = item.TblCourse.Title;
                vm.CourseLink= "/CoursePreview/" + (item.TblCourse.ShortLink != null ? item.TblCourse.ShortLink.Replace(" ", "-") : item.TblCourse.Title.Replace(" ", "-")) + "/" + item.ID;
                if (item.TblUserSender.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.TblUserSender.ImageID);
                    vm.UserImage = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                var review = qReview.Where(a => a.CourseID == item.CourseID).Where(a => a.UserID == item.UserSender).FirstOrDefault();
                if (review != null)
                {
                    vm.Rating = (decimal)review.Rating;
                }
                else
                {
                    vm.Rating = 5;
                }
                LstVm.Add(vm);
            }
            return LstVm;
        }
        public List<BlogAds> GetBlogAds(string AdsList)
        {
            List<BlogAds> vm = new List<BlogAds>();
            var qAds = db.TblSlide.Where(a => a.Section == "blog-ad").ToList();
            if (AdsList != null)
            {
                string[] ads = AdsList.Split(new char[] { ',' });
                foreach (var tItem in qAds)
                {
                    foreach (var itemSub in ads)
                    {
                        if (itemSub != "" && Convert.ToInt32(itemSub) == tItem.ID)
                        {
                            BlogAds ba = new BlogAds();
                            ba.ID = tItem.ID;
                            ba.Description = tItem.Description;
                            vm.Add(ba);
                        }
                    }
                }
            }
            return vm;
        }
        public void AddVisit(int PostID)
        {
            var qNews = db.TblNews.Where(a => a.ID == PostID).SingleOrDefault();
            qNews.Visit += 1;
            db.Update(qNews);
            db.SaveChanges();
        }
        public List<TblSlide> GetMainSlide()
        {
            var qSlide = db.TblSlide.Where(a => a.Section == "main-slide").AsNoTracking().ToList();

            return qSlide;
        }
        public List<TblSlide> GetTeam(int Count)
        {
            var qSlide = db.TblSlide.Where(a => a.Section == "team").Where(a=>a.Enable==true).Take(Count).ToList();

            return qSlide;
        }
        public List<TblSlide> GetPreviewSlide()
        {
            var qSlide = db.TblSlide.Where(a => a.Section == "preview-slide").AsNoTracking().ToList();

            return qSlide;
        }
        public List<TblSlide> GetQuotes()
        {
            var qSlide = db.TblSlide.Where(a => a.Section == "quote").AsNoTracking().ToList();

            return qSlide;
        }
        public TblSlide GetIntroVideo()
        {
            var qSlide = db.TblSlide.Where(a => a.Section == "intro").FirstOrDefault();

            return qSlide;
        }
        public int GetStudentCount()
        {
            string RoleID = db.Roles.Where(a => a.Name == "Student").SingleOrDefault().Id;
            int count = db.UserRoles.Where(a=>a.RoleId==RoleID).Count();

            return count;
        }
        public int GetVideoCount()
        {
            int count = db.TblVideo.Count();
            return count;
        }
        public TblSocial GetSocials()
        {
            var qSocials = db.TblSocial.FirstOrDefault();
            return qSocials;
        }
        public bool CheckVideoAccess(string UserId,int VideoID,bool IsAdmin)
        {
            var qVideo =  db.TblVideo.Where(a => a.ID == VideoID)
                 .Include(a => a.TblSession).ThenInclude(a=>a.TblCourse).SingleOrDefault();

            var qUserLevel = db.TblUserLevels.Where(a => a.UserID == UserId && a.CourseID == qVideo.TblSession.CourseID)
                .Where(a => a.Level == qVideo.TblSession.Level).FirstOrDefault();

            var userSBuy = db.TblUserSession.Where(a => a.SessionID == qVideo.SessionID).FirstOrDefault();

            if(qVideo.TblSession.IsFree || userSBuy!=null || qUserLevel!=null || qVideo.TblSession.TblCourse.TeacherID == UserId || IsAdmin)
            {
                return true;
            }
            return false;
        }
        public TblBanners GetSingleBanner(string Section)
        {
            var qBanner = db.TblBanners.Where(a => a.Section == Section).FirstOrDefault();
            return qBanner;
        }
        public string GetJsonFaq(List<TblGuide> model)
        {
            string text = "";
            foreach (var item in model)
            {
                if (item.ID != model.FirstOrDefault().ID)
                {
                    text += "{" +
                                      "\"@type\": \"Question\"," +
                                      "\"name\": \"" + item.Title.Replace("\"", "\\\"") + "\"," +
                                      "\"acceptedAnswer\": {" +
                                        "\"@type\": \"Answer\"," +
                                        "\"text\": \"" + WebUtility.HtmlEncode(item.Text.Replace("\"", "\\\"")) + "\"" +
                                      "}";
                    text += "}" + (item.ID != model.Last().ID ? "," : "") + "";
                }
            }
            return text/*.Replace("'", "\"")*/;
        }

        //public string GetShortcodes(string document)
        //{
        //    int indexPos = document.IndexOf("Toggle");
        //    if (indexPos > 0)
        //    {
        //        int startPos = document.LastIndexOf("[Toggle") + "[Toggle".Length + 1;
        //        int length = document.IndexOf("[/Toggle]") - startPos;
        //        string sub = "[Toggle "+ document.Substring(startPos, length);

        //        int startTitle = sub.IndexOf("title='") + "title='".Length + 1;
        //        int lengthTitle = sub.IndexOf("|") - startTitle;
        //        string subTitle = sub.Substring(startTitle, lengthTitle);

        //        string text = "<div class='toggle toggle-primary' data-plugin-toggle><section class='toggle active'>" +
        //                        "<a class='toggle-title'>"+ subTitle + "</a><div class='toggle-content'>" +
        //                         "<p>-</p></div></section></div>";

        //        string re = @"[^\x09\x0A\x0D\x20-\xD7FF\xE000-\xFFFD\x10000-x10FFFF]";
        //        //Regex.Replace(text, re,"");
        //        //remove hexadecimal characters from string c#
        //        document = Regex.Replace(document.Replace(sub,text), re, "");
        //    }

        //    return document;
        //}
        ~HomeRepository()
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
