using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryWPF.Models
{
    public class LibraryModel
    {
        public int Id { get; set; }
        public string AuthorFirstName { get; set; }
        public string AuthorLastName { get; set; }
        public string AuthorSurName { get; set; }

        [Column(TypeName = "Date")]
        public DateTime AuthorBirthDate { get; set; }
        public string BookName { get; set; }

        public int BookYear { get; set; }

    }
}
