using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmBlog
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public string UserName { get; set; }
        public string UserFullName { get; set; }
        public string UserDescription { get; set; }
        public string UserImage { get; set; }
        public string Title { get; set; }
        public string Text { get; set; }
        public string Description { get; set; }
        public string Link { get; set; }
        public string ImageUrl { get; set; }
        public string Date { get; set; }
        public string MonthOfDate { get; set; }
        public string DayOfDate { get; set; }
        public string OrginalDate { get; set; }
        public int Visit { get; set; }
        public decimal Rating { get; set; }
        public int RatingCount { get; set; }
        public bool Pin { get; set; }

        public List<BlogComment> LstComments { get; set; }
        public List<BlogCategory> LstCategory { get; set; }
        public List<BlogTags> LstTags { get; set; }
        public List<BlogAds> LstAds { get; set; }

    }
    public class BlogAds
    {
        public int ID { get; set; }
        public string Description { get; set; }
    }
    public class BlogComment
    {
        public int ID { get; set; }
        public int SubCommentID { get; set; }
        public int ReplyID { get; set; }
        public bool IsAdmin { get; set; }
        public string SenderName { get; set; }
        public string ReplyTo { get; set; }
        public string Date { get; set; }
        public string Text { get; set; }
        public string ProfileImage { get; set; }
    }
    public class BlogTags
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }
    public class BlogCategory
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }
    public class VmBlogReview
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string CourseLink { get; set; }
        public decimal Rating { get; set; }
        public string UserFullName { get; set; }
        public string UserImage { get; set; }
        public string UserID { get; set; }
        public bool ISBuy { get; set; }
        public string Text { get; set; }
    }
}
