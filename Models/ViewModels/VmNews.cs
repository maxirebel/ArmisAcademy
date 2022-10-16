using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmNews
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Writer { get; set; }
        public string WriterImage { get; set; }

        public string Categgory { get; set; }
        public string Link { get; set; }
        public int Visit { get; set; }
        public string Text { get; set; }
        public string Date { get; set; }
        public string MonthOfDate { get; set; }
        public string DayOfDate { get; set; }
        public decimal Rating { get; set; }
        public string ImageUrl { get; set; }
    }
}
