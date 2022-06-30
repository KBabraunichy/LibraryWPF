using LibraryWPF.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace LibraryWPF.Repositories
{
    public class LibraryRepository : IRepository<LibraryModel>
    {
        private LibraryContext db;

        public LibraryRepository()
        {
            db = new LibraryContext();
        }

        public ObservableCollection<LibraryModel> GetObjectCollection()
        {
            return new ObservableCollection<LibraryModel>(db.Library);
        }

        public LibraryModel GetObject(int id)
        {
            return db.Library.Find(id);
        }

        public void Insert(LibraryModel obj)
        {
            db.Library.Add(obj);
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