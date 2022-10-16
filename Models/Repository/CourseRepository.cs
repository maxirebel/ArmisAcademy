using ArmisApp.Models.Domain.context;
using ArmisApp.Models.Domain.db;
using ArmisApp.Models.ExMethod;
using ArmisApp.Models.Identity;
using ArmisApp.Models.Utility;
using ArmisApp.Models.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace ArmisApp.Models.Repository
{
    public class CourseRepository : IDisposable
    {
        private DataContext db = null;

        public CourseRepository()
        {
            db = new DataContext();
        }
        public List<VmCourses> GetLastCourses(int Take = 2)
        {
            var qCourse = db.TblCourse.Where(a => a.Status == 1).AsQueryable();

            qCourse.Include(t => t.TblGroup).Load();
            qCourse.Include(t => t.TblUser).Load();
            qCourse.Include(a => a.TblLevelPrice).Load();
            qCourse = qCourse.OrderByDescending(a => a.Date).Take(Take);

            List<VmCourses> lstCourse = new List<VmCourses>();

            var DataList = GetCourseListData(qCourse);
            lstCourse.AddRange(DataList);

            return lstCourse;
        }
        public List<VmOnlineCourse> GetLastOnlineCourses(int Take = 2,int Type=1)
        {
            var qCourse = db.TblOnlineCourse.Where(a => a.Status == 1).AsQueryable();

            qCourse.Include(t => t.TblOnlineGroup).Load();
            qCourse.Include(t => t.TblUser).Load();
            qCourse.Include(t => t.TblOnlineCoursePrice).Load();
            qCourse = qCourse.Where(a=>a.Type==Type).OrderByDescending(a => a.Date).Take(Take);

            List<VmOnlineCourse> lstCourse = new List<VmOnlineCourse>();

            var DataList = GetOnlineCourseListData(qCourse);
            lstCourse.AddRange(DataList);

            return lstCourse;
        }
        public List<VmCourses> GetShortLastCourses(int Take = 3)
        {
            var qCourse = db.TblCourse.Where(a => a.Status == 1)
                .Include(t => t.TblUser)
                .OrderByDescending(a => a.Date).Take(Take).AsQueryable();
            List<VmCourses> lstCourse = new List<VmCourses>();
            FileRepository RepImg = new FileRepository();

            var DataList = GetShortCourseListData(qCourse);
            lstCourse.AddRange(DataList);

            return lstCourse;
        }

        public List<VmCourses> GetRelatedBlogCourse(int BlogID, int Take = 2)
        {
            var qBlog = db.TblNews_Key.Where(a => a.NewsID == BlogID).Include(a => a.TblKeyword);

            List<VmCourses> lstCourse = new List<VmCourses>();
            FileRepository RepImg = new FileRepository();
            foreach (var itemKey in qBlog)
            {
                var qCourse = db.TblCourse_Key.Where(a => a.TblKeyword.Title.Contains(itemKey.TblKeyword.Title)).Where(a => a.TblCourse.Status == 1)
                    .Include(a => a.TblCourse).ThenInclude(a => a.TblUser)
                    .Include(a => a.TblCourse).ThenInclude(a => a.TblLevelPrice)
                    .OrderByDescending(a => a.TblCourse.Date).Take(Take).ToList();

                foreach (var item in qCourse)
                {
                    if (lstCourse.Where(a => a.ID == item.TblCourse.ID).Count() > 0)
                    {
                        break;
                    }
                    VmCourses vm = new VmCourses();
                    vm.ID = item.TblCourse.ID;
                    vm.Title = item.TblCourse.Title;
                    //vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                    vm.Link = "/CoursePreview/" + (item.TblCourse.ShortLink != null ? item.TblCourse.ShortLink.Replace(" ", "-") : item.TblCourse.Title.Replace(" ", "-")) + "/" + item.TblCourse.ID;
                    if (item.TblCourse.ImageID != null)
                    {
                        var Image = RepImg.GetImageByID(item.TblCourse.ImageID);
                        vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                    }
                    if (item.TblCourse.TblLevelPrice.Count() > 0)
                    {
                        int TotalPrice = item.TblCourse.TblLevelPrice.Sum(a => a.Prise);
                        vm.BasePrice = item.TblCourse.TblLevelPrice.FirstOrDefault().Prise.ToString("N0").ConvertNumerals();
                        vm.TotalPrice = TotalPrice.ToString("N0").ConvertNumerals();
                        if (item.TblCourse.DiscountPercent > 0)
                        {
                            //محاسبه تخفیف بر اساس درصد
                            int DiscountPrice = ((item.TblCourse.DiscountPercent * (TotalPrice)) / 100).Value;
                            vm.Price = ((TotalPrice - DiscountPrice)).ToString("N0").ConvertNumerals() + " تومان";
                        }
                        else
                        {
                            vm.TotalPrice = (item.TblCourse.TblLevelPrice.FirstOrDefault().Prise).ToString("N0").ConvertNumerals() + " تومان";
                        }
                    }
                    lstCourse.Add(vm);
                }
            }
            return lstCourse.Take(Take).ToList();
        }
        public List<VmCourses> GetCourseListData(IQueryable<TblCourse> qCourse)
        {
            List<VmCourses> lstCourse = new List<VmCourses>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            
            foreach (var item in qCourse)
            {
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.TeacherUserName = item.TblUser.UserName;
                vm.Status = item.Status;
                vm.Date = Time.GetTimeName(item.Date);
                vm.NormalDate = item.Date;
                vm.Visit = item.Visit;
                vm.LastUpdate = Time.GetTimeName(item.LastUpdate);
                var groupID = item.TblGroup.ID;
                vm.GroupName = db.TblGroup.Where(a => a.ID == groupID).FirstOrDefault().Title;
                vm.SubGroupName = item.TblGroup.Title;
                var price = item.TblLevelPrice;
                vm.Link = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.ShortLink = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.TotalPrice = "تعیین نشده";
                if (item.TblLevelPrice.Count() > 0)
                {
                    int TotalPrice = item.TblLevelPrice.Sum(a => a.Prise);
                    vm.BasePrice = item.TblLevelPrice.FirstOrDefault().Prise.ToString("N0").ConvertNumerals();
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
                if (item.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.ImageID);
                    vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                var TeacherImage = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;

                var qReview = db.TblCourseReview.Where(a => a.CourseID == item.ID);
                if (qReview.Count() > 0)
                {
                    decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                    vm.Rating = (decimal)Math.Round(Avrage, 1);
                    vm.RatingCount = qReview.Count();
                }
                #region VideoTime
                vm.TotalTime = GetCourseTime(item.ID);
                #endregion
                lstCourse.Add(vm);
            }
            return lstCourse;
        }
        public List<VmOnlineCourse> GetOnlineCourseListData(IQueryable<TblOnlineCourse> qCourse)
        {
            List<VmOnlineCourse> lstCourse = new List<VmOnlineCourse>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();
            PayRepository RepPay = new PayRepository();

            foreach (var item in qCourse)
            {
                VmOnlineCourse vm = new VmOnlineCourse();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.TeacherUserName = item.TblUser.UserName;
                vm.Status = item.Status;
                vm.Date = Time.GetTimeName(item.Date);
                vm.Visit = item.Visit;
                vm.Type = item.Type;
                vm.TotalPrice = item.TotalPrice;
                vm.SessionsCount = item.SessionsCount;
                vm.Link = "/ClassView/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                if (item.Type == 1)
                {
                    if (item.StartDate.Year != 0001)
                    {
                        vm.StartDate = item.StartDate.ToShamsi().ToString("yyyy/MM/dd").ConvertNumerals();
                    }
                    else
                    {
                        vm.StartDate = "تعیین نشده";
                    }
                    if (item.EndDate.Year != 0001)
                    {
                        vm.EndDate = item.EndDate.ToShamsi().ToString("yyyy/MM/dd").ConvertNumerals();
                    }
                    else
                    {
                        vm.EndDate = "تعیین نشده";
                    }
                }
                else if (item.Type == 2)
                {
                    if (item.TblOnlineCoursePrice != null)
                    {
                        vm.TotalPrice = item.TblOnlineCoursePrice.Price;
                    }
                    RepPay.GetPackageViewPrice(item.TblOnlineCoursePrice, vm);
                }

                var groupID = item.TblOnlineGroup.GroupID;
                var subGroup = db.TblOnlineGroup.Where(a => a.ID == groupID).FirstOrDefault();
                if (subGroup != null)
                {
                    vm.GroupName = subGroup.Title;
                    vm.SubGroupName = item.TblOnlineGroup.Title;
                }
                else
                {
                    vm.GroupName = item.TblOnlineGroup.Title;
                    vm.SubGroupName = item.TblOnlineGroup.Title;
                }
                //vm.ShortLink = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                if (item.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.ImageID);
                    vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                else
                {
                    vm.Image = "/assets/media/misc/image2.png";
                }
                var TeacherImage = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;
                lstCourse.Add(vm);
            }
            return lstCourse;
        }

        public List<VmCourses> GetShortCourseListData(IQueryable<TblCourse> qCourse)
        {
            List<VmCourses> lstCourse = new List<VmCourses>();
            TimeUtility Time = new TimeUtility();
            FileRepository RepImg = new FileRepository();

            foreach (var item in qCourse)
            {
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.TeacherUserName = item.TblUser.UserName;
                vm.Date = Time.GetTimeName(item.Date);
                vm.Link = "/CoursePreview/" + (item.ShortLink != null ? item.ShortLink.Replace(" ", "-") : item.Title.Replace(" ", "-")) + "/" + item.ID;
                vm.TotalPrice = "تعیین نشده";
                if (item.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.ImageID);
                    vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                var TeacherImage = RepImg.GetImageByID(item.TblUser.ImageID);
                vm.TeacherImg = TeacherImage.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + TeacherImage.TblServer.Path.Trim(new char[] { '/' }) + "/" + TeacherImage.FileName;
                lstCourse.Add(vm);
            }
            return lstCourse;
        }

        public List<VmCourses> GetLastTeacherCourse(string UserID)
        {
            var qCourse = db.TblCourse.Include(t => t.TblGroup).Where(a => a.Status == 1)
                .Where(a => a.TeacherID == UserID)
                .Include(t => t.TblUser)
                .Include(a => a.TblUserCourse)
                .OrderByDescending(a => a.Date).Take(5).ToList();
            List<VmCourses> lstCourse = new List<VmCourses>();
            var Course = qCourse.ToList();
            foreach (var item in Course)
            {
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.Title;
                vm.Teacher = item.TblUser.FirstName + " " + item.TblUser.LastName;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - hh:mm");
                var GroupID = item.TblGroup.GroupID;
                vm.GroupName = db.TblGroup.Where(a => a.ID == GroupID).FirstOrDefault().Title;
                vm.TotalSubmited = item.TblUserCourse.Count();
                vm.SubGroupName = item.TblGroup.Title;
                vm.Status = item.Status;
                lstCourse.Add(vm);
            }
            return lstCourse;
        }
        public List<VmCourses> GetLastSubmittedCourse(string UserID)
        {
            var qCourse = db.TblUserCourse.Where(a => a.UserID == UserID)
                .Include(t => t.TblCourse).ThenInclude(a => a.TblGroup)
                .Include(t => t.TblCourse).ThenInclude(a => a.TblUser)
                .Include(t => t.TblUser)
                .OrderByDescending(a => a.Date).Take(5).ToList();
            List<VmCourses> lstCourse = new List<VmCourses>();
            FileRepository RepImg = new FileRepository();
            foreach (var item in qCourse)
            {
                VmCourses vm = new VmCourses();
                vm.ID = item.ID;
                vm.Title = item.TblCourse.Title;
                vm.Teacher = item.TblCourse.TblUser.FirstName + " " + item.TblCourse.TblUser.LastName;
                vm.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd - hh:mm");
                var GroupID = item.TblCourse.TblGroup.GroupID;
                vm.GroupName = db.TblGroup.Where(a => a.ID == GroupID).FirstOrDefault().Title;
                vm.SubGroupName = item.TblCourse.TblGroup.Title;
                vm.Link = "/CoursePreview/" + (item.TblCourse.ShortLink != null ? item.TblCourse.ShortLink.Replace(" ", "-") : item.TblCourse.Title.Replace(" ", "-")) + "/" + item.CourseID;
                if (item.TblCourse.ImageID != null)
                {
                    var Image = RepImg.GetImageByID(item.TblCourse.ImageID);
                    vm.Image = Image.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + Image.TblServer.Path.Trim(new char[] { '/' }) + "/" + Image.FileName;
                }
                lstCourse.Add(vm);
            }
            return lstCourse;
        }
        public List<PurchasedLevels> GetLastLevelsBuy(string UserID)
        {
            var qLevels = db.TblTransaction.Where(a => a.UserID == UserID)
                .Where(a => a.TblInvoice.CourseID > 0)
                .Where(a => a.Type == 1)
                .Include(a => a.TblInvoice)
                .OrderByDescending(a => a.Date).Take(8);
            List<PurchasedLevels> vm = new List<PurchasedLevels>();
            foreach (var item in qLevels)
            {
                var qCourse = db.TblCourse.Where(a => a.ID == item.TblInvoice.CourseID).SingleOrDefault();
                PurchasedLevels pl = new PurchasedLevels();
                pl.Level = Convert.ToInt32(item.TblInvoice.Level);
                pl.Time = item.Date.ToString("hh:mm");
                pl.Date = item.Date.ToShamsi().ToString("yyyy/MM/dd");
                pl.Link = "/Course/" + (qCourse.ShortLink != null ? qCourse.ShortLink.Replace(" ", "-") : qCourse.Title.Replace(" ", "-")) + "/" + qCourse.ID;
                pl.CourseName = db.TblCourse.Where(a => a.ID == item.TblInvoice.CourseID).SingleOrDefault().Title;
                vm.Add(pl);
            }
            return vm;
        }
        public List<Group> GetCourseGroups()
        {
            var qGroup = db.TblGroup.Where(a => a.GroupID == 0).ToList();
            List<Group> LstGroup = new List<Group>();
            foreach (var item in qGroup)
            {
                Group gp = new Group();
                gp.ID = item.ID;
                gp.Title = item.Title;

                LstGroup.Add(gp);
            }
            return LstGroup;
        }
        public List<TblVideo> GetVideoFormats(int Number)
        {
            var qFormat = db.TblVideo.Where(a => a.Number == Number).ToList();

            return qFormat;
        }
        //public int GetSection(int Level,int CourseID)
        //{
        //    var qCourse = db.TblCourse.Where(a => a.ID == CourseID).SingleOrDefault();
        //    int SectionID = 0;

        //    if (Level <= qCourse.CIntroductory)
        //    {
        //        // مقدماتی
        //        SectionID = 1;
        //    }
        //    else if (Level > qCourse.CIntroductory && Level <= qCourse.CIntroductory + qCourse.CMedium)
        //    {
        //        // متوسط
        //        SectionID = 2;
        //    }
        //    else if (Level > qCourse.CIntroductory && Level > qCourse.CIntroductory + qCourse.CMedium
        //        && Level <= qCourse.CIntroductory + qCourse.CMedium + qCourse.CAdvanced)
        //    {
        //        // پیشرفته
        //        SectionID = 3;
        //    }
        //    return SectionID;
        //}
        public CourseRating GetCourseRating(int CourseID)
        {
            CourseRating vm = new CourseRating();
            var qReview = db.TblCourseReview.Where(a => a.CourseID == CourseID);
            if (qReview.Count() > 0)
            {
                decimal Avrage = (decimal)qReview.Sum(a => a.Rating) / qReview.Count();
                vm.Rating = (decimal)Math.Round(Avrage, 1);
                vm.RatingCount = qReview.Count();

                vm.FiveStar = Math.Round(((decimal)qReview.Where(a => a.Rating == 5).Count() * 100) / qReview.Count(), 1);
                vm.FourStar = Math.Round(((decimal)qReview.Where(a => a.Rating == 4).Count() * 100) / qReview.Count(), 1);
                vm.ThreeStar = Math.Round(((decimal)qReview.Where(a => a.Rating == 3).Count() * 100) / qReview.Count(), 1);
                vm.TwoStar = Math.Round(((decimal)qReview.Where(a => a.Rating == 2).Count() * 100) / qReview.Count(), 1);
                vm.OneStar = Math.Round(((decimal)qReview.Where(a => a.Rating == 1).Count() * 100) / qReview.Count(), 1);
            }
            return vm;
        }
        public VmSelect GetLevelTime(int CourseID, int Level)
        {

            VmSelect vm = new VmSelect();
            TimeSpan FullTime = new TimeSpan();
            var qVid = db.TblVideo.Where(a => a.TblSession.CourseID == CourseID).Where(a => a.TblSession.Level == Level);
            List<string> UniqueCodes = new List<string>();
            List<int> se = new List<int>();
            foreach (var itemTime in qVid)
            {
                if (!UniqueCodes.Any(a => a == itemTime.UniqueCode))
                {
                    vm.ID += 1;
                    if (itemTime.Time != null && itemTime.Time.Contains(":"))
                    {
                        string[] cTime = itemTime.Time.Split(new string[] { ":" }, StringSplitOptions.None);
                        TimeSpan t1 = TimeSpan.Parse("00:" + cTime[0] + ":" + cTime[1]);

                        FullTime += t1;
                        se.Add((int)itemTime.SessionID);
                        UniqueCodes.Add(itemTime.UniqueCode);
                    }
                }
            }
            vm.Title = FullTime.ToString();
            return vm;
        }
        public string GetCourseTime(int CourseID)
        {
            TimeSpan FullTime = new TimeSpan();
            var qVid = db.TblVideo.Where(a => a.TblSession.CourseID == CourseID);
            List<string> UniqueCodes = new List<string>();
            List<int> se = new List<int>();
            foreach (var itemTime in qVid)
            {
                if (!UniqueCodes.Any(a => a == itemTime.UniqueCode))
                {
                    if (itemTime.Time != null && itemTime.Time.Contains(":"))
                    {
                        string[] cTime = itemTime.Time.Split(new string[] { ":" }, StringSplitOptions.None);
                        TimeSpan t1 = TimeSpan.Parse("00:" + cTime[0] + ":" + cTime[1]);

                        FullTime += t1;
                        se.Add((int)itemTime.SessionID);
                        UniqueCodes.Add(itemTime.UniqueCode);
                    }
                }
            }
            return FullTime.ToString();
        }
        public string GetCourseTime2(int CourseID)
        {
            TimeSpan FullTime = new TimeSpan();
            var qVid = db.TblVideo.Where(a => a.TblSession.CourseID == CourseID);
            List<string> Number = new List<string>();
            //List<int> se = new List<int>();
            foreach (var itemTime in qVid)
            {
                if (!Number.Any(a => a.Split(new string[] { "|" }, StringSplitOptions.None)[0] == itemTime.Number.ToString()) ||
                    !Number.Any(a => a.Split(new string[] { "|" }, StringSplitOptions.None)[1] == itemTime.SessionID.ToString()))
                {
                    if (itemTime.Time != null && itemTime.Time.Contains(":"))
                    {
                        string[] cTime = itemTime.Time.Split(new string[] { ":" }, StringSplitOptions.None);
                        TimeSpan t1 = TimeSpan.Parse("00:" + cTime[0] + ":" + cTime[1]);

                        FullTime += t1;
                        //se.Add((int)itemTime.SessionID);
                        Number.Add(itemTime.Number.ToString() + "|" + itemTime.SessionID.ToString());
                    }
                }
            }
            return FullTime.ToString();
        }
        public void GetCourseFaq(VmCourses vm,DataContext _context,TblCourse qCourse)
        {
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
        }
        public void GetCourseLevelContent(int levelCount,TblCourse qCourse,DataContext _context,VmCourses vm, string UserID,bool IsAdminRole
            ,TblUserCourse qFullBuy, IHttpContextAccessor _accessor)
        {
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
                    lv.ISBNCode = lPrice.ISBNCode != null ? lPrice.ISBNCode : "";
                }
                lv.Number = i;
                #endregion LevelTitle
                #region محتوای جلسات
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

                        string IpAddress = "";
                        if (lv.IsBuy || LevelBuy != null || IsAdminRole || UserID == qCourse.TeacherID || FreeSession)
                        {
                            seItem.IsAccsess = true;
                            IpAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                            //string test = "185.120.195.138";

                        }
                        // نمایش لیست ویدئو های هر جلسه
                        seItem.LstVideo = new List<SessionVideos>();
                        TimeSpan FullTime = new TimeSpan();
                        string Number = "";

                        var SecurityIp = IpAddress.ConvertScurity();
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

                            string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes(itemVid.ID + "&" + SecurityIp + "&" + itemVid.Token));
                            SecurityKey = svcCredentials;
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
                #endregion
                vm.LstLevel.Add(lv);
            }
        }
        public void GetCourseConversion(VmCourses vm,DataContext _context, string UserID,TblCourse qCourse, IHttpContextAccessor _accessor
            ,TimeUtility Time,FileRepository RepImg)
        {
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
                    string IpAddress = _accessor.HttpContext.Connection.RemoteIpAddress.ToString();
                    //string test = "185.120.195.138";

                    var SecurityIp = IpAddress.ConvertScurity();
                    string svcCredentials = Convert.ToBase64String(ASCIIEncoding.ASCII.GetBytes("Armis" + "&" + SecurityIp));
                    string SecurityKey = svcCredentials;

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
                    string Path = file.TblServer.HttpDomain.Trim(new char[] { '/' }) + "/" + file.TblServer.Path.Trim(new char[] { '/' }) + "/" + file.FileName;
                    sc.File.Link = Path + "?u=" + SecurityKey;
                    //sc.File.Link = "/dl/file/" + item.TblFiles.ID + "/" + item.TblFiles.Token;
                    sc.File.Title = item.TblFiles.Title;
                }
                vm.LstConversion.Add(sc);
            }
        }
        public int TotalVideo(int CourseID)
        {
            int total = 0;
            var qVid = db.TblVideo.Where(a => a.TblSession.CourseID == CourseID).Include(a => a.TblSession);

            List<int> Se = new List<int>();
            List<int> Number = new List<int>();
            List<string> UniqueCodes = new List<string>();
            foreach (var item in qVid)
            {
                if (!UniqueCodes.Any(a => a == item.UniqueCode))
                {
                    total++;
                    UniqueCodes.Add(item.UniqueCode);
                }
            }
            return total;
        }

        public string GetJsonReviews(List<SessionComments> model)
        {
            string text = "";
            foreach (var item in model.Take(3))
            {
                text += "{" +
                  "'@type': 'Review'," +
                  "'author': '" + item.FullName + "'," +
                  "'datePublished': '" + item.MiladiDate + "'," +
                  "'reviewBody': '" + /*WebUtility.HtmlEncode()*/ item.Text + "'," +
                  "'name': '" + item.FullName + "'";
                if (item.Rating > 0)
                {
                    text += ",'reviewRating': {" +
                    "'@type': 'Rating'," +
                    "'bestRating': '5'," +
                    "'ratingValue': '"+item.Rating+"'," +
                    "'worstRating': '1'" +
                  "}";
                }
                text += "}"+(item.ID!= model.Take(3).Last().ID?",":"") +"";
            }
            return text.Replace("'", "\"");
        }
        ~CourseRepository()
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
