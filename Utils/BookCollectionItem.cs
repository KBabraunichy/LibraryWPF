using LibraryWPF.Models;
using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace LibraryWPF.Utils
{
    public class BookCollectionItem
    {
        public Author Author { get; set; }
        public Book Book { get; set; }

        public override string ToString()
        {
            return Author.ToString() + ';' + Book.ToString();
        }

    }
}
