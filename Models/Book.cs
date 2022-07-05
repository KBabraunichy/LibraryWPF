using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryWPF.Models
{
    public class Book
    {
        public int Id { get; set; }
        public string BookName { get; set; }

        public int BookYear { get; set; }

        public int AuthorId { get; set; }

        [ForeignKey("AuthorId")]
        public Author Author { get; set; }
        
        public override string ToString()
        {
            return BookName + ';' + BookYear;
        }

    }


}
