using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryWPF.Models
{
    public class LibraryModel
    {
        public Author AuthorLib { get; set; }
        public Book BookLib { get; set; }

        public override string ToString()
        {
            return AuthorLib.ToString() + ';' + BookLib.ToString();
        }

    }


}
