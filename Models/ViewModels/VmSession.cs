using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmSession
    {
        public int ID { get; set; }
        public int CourseID { get; set; }
        public string Title { get; set; }
        public int Price { get; set; }
        public int VideoCount { get; set; }
        public bool IsFree { get; set; }
        public string Description { get; set; }
        public string FileDescription { get; set; }
        public Files SessionFile { get; set; }
        public List<Videos> LstVideo { get; set; }
    }
    public class Videos
    {
        public int ID { get; set; }
        public int Quality { get; set; }
        public int Number { get; set; }
        public string Url { get; set; }
        public string Title { get; set; }
        public string FileName { get; set; }
        public string Time { get; set; }
        public string PosterPath { get; set; }
        public int Sort { get; set; }
        public string Alt { get; set; }
    }
    public class Files
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Alt { get; set; }
    }
}
