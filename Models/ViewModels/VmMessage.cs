using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmMessage
    {
        public int ID { get; set; }
        public string Text { get; set; }
        public string SenderID { get; set; }
        public string SenderName { get; set; }
        public bool Read { get; set; }
        public string ProfileImage { get; set; }
        public string CourseName { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
        public string LastSentDate { get; set; }
        public string DateName { get; set; }
        public string Link { get; set; }
        public string Type { get; set; }
        public int Level { get; set; }
        public MsgFile File { get; set; }
    }
    public class MsgFile
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }
}
