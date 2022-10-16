using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ArmisApp.Models.ViewModels
{
    public class VmTicket
    {
        public int ID { get; set; }
        public string UserName { get; set; }
        public string UserID { get; set; }
        public string IPAdresss { get; set; }
        public string ProfileImage { get; set; }
        public string SenderName { get; set; }
        public string CourseName { get; set; }
        public string Title { get; set; }
        public string Email { get; set; }
        public string Mobile { get; set; }
        public string UserDetails { get; set; }
        public string Date { get; set; }
        public string Link { get; set; }
        public bool BeNotified { get; set; }
        public int Status { get; set; }
        public bool Read { get; set; }
        public int Priority { get; set; }

        public List<TicketMsg> ListTicketMsg { get; set; }

    }
    public class TicketMsg
    {
        public string SenderID { get; set; }

        public string SenderName { get; set; }

        public string ProfileImage { get; set; }

        public string Date { get; set; }

        public string Text { get; set; }
        public MessageFile File { get; set; }
    }
    public class MessageFile
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Link { get; set; }
    }
}
