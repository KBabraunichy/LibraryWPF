using LibraryWPF.Models;
using System.Collections.ObjectModel;

namespace LibraryWPF.Repositories
{
    public class BookRepository : IRepository<Book>
    {
        private LibraryContext db;

        public BookRepository()
        {
            db = new LibraryContext();
        }

        public ObservableCollection<Book> GetObjectCollection()
        {
            return new ObservableCollection<Book>(db.Books);
        }

        public Book GetObject(int id)
        {
            return db.Books.Find(id);
        }

        public void Insert(Book obj)
        {
            db.Books.Add(obj);
        }

        public void Save()
        {
            if (HasUnsavedChanges())
                db.SaveChanges();
        }
        public bool HasUnsavedChanges()
        {
            return db.ChangeTracker.HasChanges();
        }

    }
}