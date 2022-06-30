using System.Collections.ObjectModel;

interface IRepository<T>
{
    public ObservableCollection<T> GetObjectCollection();
    public T GetObject(int id);
    public void Insert(T item);
    public void Save();
    public bool HasUnsavedChanges();

}