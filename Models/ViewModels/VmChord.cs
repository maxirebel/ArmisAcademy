using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmChord
    {
        public int ID { get; set; }
        public string Writer { get; set; }
        public bool Enable { get; set; }
        public string Publisher { get; set; }
        public string ChordTitle { get; set; }
        public string ArtistName { get; set; }
        public string ArtistEnName { get; set; }
        public string Text { get; set; }
        public string ImageUrl { get; set; }
        public string Date { get; set; }
        public string MainStep { get; set; }
        public string Rhythm { get; set; }
        public string SocialLink { get; set; }
        public string DownloadLink { get; set; }
        public int Visit { get; set; }
        public int Rating { get; set; }
        public int RatingCount { get; set; }

        public List<ChordComment> LstComments { get; set; }
        public List<ChordLearns> LstLearn { get; set; }
        public List<ChordAd> LstAd { get; set; }

    }
    public class ChordLearns
    {
        public int ID { get; set; }
        public int Sort { get; set; }
        public int Number { get; set; }
        public int Type { get; set; }
        public string UserFullName { get; set; }
        public string UserName { get; set; }
        public string UserImage { get; set; }
        public string Title { get; set; }
        public string Price { get; set; }
        public string Link { get; set; }
        public string DemoLink { get; set; }
        public string Description { get; set; }
        public bool IsBuy { get; set; }
    }
    public class ChordComment
    {
        public int ID { get; set; }
        public string FullName { get; set; }
        public string CourseName { get; set; }
        public string Text { get; set; }
        public string ProfileImage { get; set; }
        public string Date { get; set; }
        public int Status { get; set; }
        public string Link { get; set; }
        public bool IsMe { get; set; }
    }
    public class ChordAd
    {
        public int ID { get; set; }
        public int Sort { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
        public string ImageUrl { get; set; }
    }
}
