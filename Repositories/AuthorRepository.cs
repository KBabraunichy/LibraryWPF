using LibraryWPF.Models;
using System.Collections.ObjectModel;

namespace LibraryWPF.Repositories
{
    public class AuthorRepository : IRepository<Author>
    {
        private LibraryContext db;

        public AuthorRepository()
        {
            db = new LibraryContext();
        }

        public ObservableCollection<Author> GetObjectCollection()
        {
            return new ObservableCollection<Author>(db.Authors);
        }

        public Author GetObject(int id)
        {
            return db.Authors.Find(id);
        }

        public void Insert(Author obj)
        {
            db.Authors.Add(obj);
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