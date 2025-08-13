using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Exam_proctor.Sessions
{
    public static class StudentSession
    {

        public static string Id { get; set; }
        public static String StudentCustomId { get; set; }
        public static string Namef { get; set; }
        public static string NameL { get; set; }
        public static string Email { get; set; }
        public static DateTime LoggedInAt { get; set; }



        public static void Clear()
        {
            Id = null; 
            StudentCustomId = null;
            Namef = null;
            NameL = null;
            Email = null;
            LoggedInAt = DateTime.Now;
        }

        public static String getId()
        {
            return Id;
        }
    }
}
