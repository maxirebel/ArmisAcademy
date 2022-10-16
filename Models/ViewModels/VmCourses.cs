using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmCourses
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string ShortLink { get; set; }
        public string Text { get; set; }
        public int GroupID { get; set; }
        public int SubGroupID { get; set; }
        public string GroupName { get; set; }
        public string SubGroupName { get; set; }
        public string TeacherUserName { get; set; }
        public string TeacherInfo { get; set; }
        public string Teacher { get; set; }
        public string TeacherID { get; set; }
        public string TeacherImg { get; set; }
        public string Date { get; set; }
        public DateTime NormalDate { get; set; }
        public string LastUpdate { get; set; }
        public int Status { get; set; }
        public int PercentSale { get; set; }
        public int CIntroductory { get; set; }
        public string CourseLevel { get; set; }
        public int DownloadFileCount { get; set; }
        public int ProgressPercent { get; set; }
        public string Prerequisites { get; set; }
        public bool InSupport { get; set; }
        public bool Placement { get; set; }
        public string Link { get; set; }
        public string Image { get; set; }
        public string ISMNCode { get; set; }
        public bool UserSubmited { get; set; }
        public int TotalSubmited { get; set; }
        public string TotalPrice { get; set; }

        public string BasePrice { get; set; }
        public string TotalTime { get; set; }
        public string MetaDescription { get; set; }
        public string Description { get; set; }
        public string MetaTag { get; set; }
        public string Keywords { get; set; }
        public string Error { get; set; }
        public string FullTime { get; set; }
        public decimal Rating { get; set; }
        public bool UserBuyer { get; set; }
        public int RatingCount { get; set; }
        public int CommentCount { get; set; }
        public string Price { get; set; }
        public int FinalPrice { get; set; }
        public string DiscountDescription { get; set; }
        public int DiscountRemaining { get; set; }

        public int Visit { get; set; }
        public int ChatID { get; set; }
        public Session Session { get; set; }

        public CourseDiscount CourseDiscount { get; set; }
        public CourseRating CourseRating  { get; set; }
        public List<Session> LstSession { get; set; }
        public List<SessionConversion> LstConversion { get; set; }
        public List<Levels> LstLevel { get; set; }
        public List<VmCourseFAQ> LstFAQ { get; set; }
        public List<CourseStudens> LstStudents { get; set; }

    }
    public class CourseStudens
    {
        public string ProfileImage { get; set; }
        public string FullName { get; set; }
    }
    public class CourseDiscount
    {
        public int LevelsPurchased { get; set; }
        public int RemainingCount { get; set; }
        public int Discount { get; set; }

    }
    public class Levels
    {
        public int ID { get; set; }
        public int Number { get; set; }
        public int SectionID { get; set; }
        public string Price { get; set; }
        public bool IsBuy { get; set; }

        // زمانی که سطح مورد نظر فاقد ویدئو می باشد
        public bool IsPending { get; set; }
        public string Title { get; set; }
        public int TotalVideo { get; set; }
        public string TotalTime { get; set; }
        public string ISBNCode { get; set; }
        public List<Session> LstSession { get; set; }
    }
    public class Teacher
    {
        public string ID { get; set; }
        public string Name { get; set; }
        public bool Selected { get; set; }
    }

    public class Session
    {
        public int ID { get; set; }
        public int Sort { get; set; }
        public int Price { get; set; }
        public int Level { get; set; }
        public int ChatID { get; set; }
        public int NextSessionID { get; set; }
        public int PreviousSessionID { get; set; }
        public int FirstVideoID { get; set; }

        public bool IsFree { get; set; }
        public bool IsAccsess { get; set; }
        public string Title { get; set; }
        public string FullTime { get; set; }
        public string Description { get; set; }
        public string FileDescription { get; set; }
        public List<SessionVideos> LstVideo { get; set; }
        public List<SessionComments> LstComments { get; set; }
        public List<SessionConversion> LstConversion { get; set; }
        public SessionFile File { get; set; }
    }
    public class SessionVideos
    {
        public int ID { get; set; }
        public int Quality { get; set; }
        public int Number { get; set; }
        public int NextVideoID { get; set; }
        public int PreviousVideoID { get; set; }
        public int Sort { get; set; }
        public string Title { get; set; }
        public string Alt { get; set; }
        public string FileName { get; set; }
        public string Link { get; set; }
        public string dlLink { get; set; }
        public string Poster { get; set; }
    }
    public class SessionFile
    {
        public int ID { get; set; }
        public string Alt { get; set; }
        public string FileName { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
    }
    public class SessionComments
    {
        public int ID { get; set; }
        public int ReplyID { get; set; }
        public string ReplyTo { get; set; }
        public string FullName { get; set; }
        public string UserName { get; set; }
        public string CourseName { get; set; }
        public string Text { get; set; }
        public string ProfileImage { get; set; }
        public string Date { get; set; }
        public string MiladiDate { get; set; }
        public int Status { get; set; }
        public string Link { get; set; }
        public bool IsMe { get; set; }
        public bool isBuyer { get; set; }
        public int BuyOffer { get; set; }
        public decimal Rating { get; set; }
    }
    public class SessionConversion
    {
        public int ID { get; set; }
        public int ChatID { get; set; }
        public string FullName { get; set; }
        public string Text { get; set; }
        public string ProfileImage { get; set; }
        public string Date { get; set; }
        public string SessionName { get; set; }
        public string Link { get; set; }
        public bool IsMe { get; set; }
        public CMessageFile File { get; set; }
    }
    public class PurchasedLevels
    {
        public int ID { get; set; }
        public int Level { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
        public string Link { get; set; }
        public string CourseName { get; set; }
    }
    public class CMessageFile
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }
    public class VmCourseFAQ
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public bool Selected { get; set; }
        public int Sort { get; set; }
    }
    public class VmCourseReview
    {
        public int ID { get; set; }
        public string CourseTitle { get; set; }
        public string CourseLink { get; set; }
        public decimal Rating { get; set; }
        public string UserFullName{ get; set; }
        public string UserImage { get; set; }
        public string UserID { get; set; }
        public bool ISBuy { get; set; }
        public string Text { get; set; }
    }
    public class CourseRating
    {
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public decimal FiveStar { get; set; }
        public int FiveStarPrecent { get; set; }
        public decimal FourStar { get; set; }
        public int FourStarPercent { get; set; }
        public decimal ThreeStar { get; set; }
        public int ThreeStarPercent { get; set; }
        public decimal TwoStar { get; set; }
        public int TwoStarPercent { get; set; }
        public decimal OneStar { get; set; }
        public int OneStarPercent { get; set; }
    }
}
