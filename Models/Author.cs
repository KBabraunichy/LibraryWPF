using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryWPF.Models
{
    public class Author
    {
        public int Id { get; set; }
        public string AuthorFirstName { get; set; }
        public string AuthorLastName { get; set; }
        public string AuthorSurName { get; set; }

        [Column(TypeName = "Date")]
        public DateTime AuthorBirthDate { get; set; }

        public override string ToString()
        {
            return AuthorFirstName + ';' + AuthorLastName + ';' + AuthorSurName + ';' + AuthorBirthDate;
        }

    }


}
