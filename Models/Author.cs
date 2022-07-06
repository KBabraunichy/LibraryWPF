using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryWPF.Models
{
    public class Author
    {
        public int Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string SurName { get; set; }

        [Column(TypeName = "Date")]
        public DateTime BirthDate { get; set; }

        public override string ToString()
        {
            return FirstName + ';' + LastName + ';' + SurName + ';' + BirthDate;
        }

    }


}
