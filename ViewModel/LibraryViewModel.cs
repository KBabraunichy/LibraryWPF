using LibraryWPF.Commands;
using LibraryWPF.Models;
using LibraryWPF.Repositories;
using LibraryWPF.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Xml.Linq;


namespace LibraryWPF.ViewModel
{
    public class LibraryViewModel : INotifyPropertyChanged
    {
        private readonly AuthorRepository _authorRepository;
        private readonly BookRepository _bookRepository;

        public LibraryViewModel()
        {
            _authorRepository = new AuthorRepository();
            _bookRepository = new BookRepository();

        }

        public ObservableCollection<LibraryModel> GetLibraryCollection() 
        {
            var authorCollection = _authorRepository.GetObjectCollection();
            var bookCollection = _bookRepository.GetObjectCollection();

            return  new ObservableCollection<LibraryModel>(
                        from authors in authorCollection
                        join books in bookCollection on authors.Id equals books.AuthorId
                        select new LibraryModel
                        {
                            AuthorLib = new Author
                            {
                                Id = authors.Id,
                                AuthorFirstName = authors.AuthorFirstName,
                                AuthorLastName = authors.AuthorLastName,
                                AuthorSurName = authors.AuthorSurName,
                                AuthorBirthDate = authors.AuthorBirthDate
                            },
                            BookLib = new Book
                            {
                                BookName = books.BookName,
                                BookYear = books.BookYear,
                                AuthorId = authors.Id
                            }
                        }
                    );
            
        }


        //--------------------------open file/import data

        private ObservableCollection<LibraryModel> _loadedCollection;
        public ObservableCollection<LibraryModel> LoadedCollection
        {
            get { return _loadedCollection; }
            set { _loadedCollection = value; }
        }

        private ICollectionView _loadedCollectionView;

        public ICollectionView LoadedCollectionView
        {
            get { return _loadedCollectionView; }
            set { _loadedCollectionView = value; }
        }
        
        public ICommand LoadCommand => new RelayCommand(ReadFile);

        private void ReadFile(object commandParameter)
        {
            LoadedCollection = new ObservableCollection<LibraryModel>();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv";

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    List<Tuple<string, string>> listErrors = new List<Tuple<string, string>>();

                    List<string> uniqueLines = new List<string>();

                    //remove duplicates in file
                    using (StreamReader streamReader = File.OpenText(openFileDialog.FileName))
                    {
                        string line;
                        while ((line = streamReader.ReadLine()) != null)
                            if (!uniqueLines.Contains(line))
                                uniqueLines.Add(line);
                    }

                    var libColection = GetLibraryCollection();
                    foreach (string line in uniqueLines)
                    {
                        try
                        {
                            string[] data = line.Split(';');

                            DateTime authorBirthDate = DateTime.Parse(data[3]).Date;

                            if (data.Length != 6)
                            {
                                listErrors.Add(new Tuple<string, string>(line, "Incorrect line format for parsing."));
                                continue;
                            }

                            if (!data[0].All(Char.IsLetter) || !data[1].All(Char.IsLetter) || !data[2].All(Char.IsLetter) ||
                                string.IsNullOrEmpty(data[0]) || string.IsNullOrEmpty(data[1]))
                            {
                                listErrors.Add(new Tuple<string, string>(line, "Incorrect string format for parsing first name, last name or surname."));
                                continue;
                            }

                            if (string.IsNullOrEmpty(data[4]))
                            {
                                listErrors.Add(new Tuple<string, string>(line, "Book name can't be empty."));
                                continue;
                            }

                            int bookYear = 0;
                            if (!string.IsNullOrEmpty(data[5]))
                            {
                                if (!int.TryParse(data[5], out bookYear) || bookYear > DateTime.Now.Date.Year || bookYear < 1)
                                {
                                    listErrors.Add(new Tuple<string, string>(line, "Incorrect book year."));
                                    continue;
                                }
                            }

                            var checkAuthorInDB = libColection.Where(a => a.AuthorLib.AuthorFirstName == data[0].Trim() && a.AuthorLib.AuthorLastName == data[1].Trim() &&
                                                                          a.AuthorLib.AuthorSurName == data[2].Trim() && a.AuthorLib.AuthorBirthDate == authorBirthDate)
                                                              .Select(aid => aid.AuthorLib.Id)
                                                              .FirstOrDefault();

                            if (checkAuthorInDB == 0)
                            {
                                Author newAuthor = new Author
                                {
                                    AuthorFirstName = data[0].Trim(),
                                    AuthorLastName = data[1].Trim(),
                                    AuthorSurName = data[2].Trim(),
                                    AuthorBirthDate = authorBirthDate,
                                };

                                Book newBook = new Book
                                {
                                    BookName = data[4],
                                    BookYear = bookYear,
                                    Author = newAuthor
                                };

                                _authorRepository.Insert(newAuthor);
                                _bookRepository.Insert(newBook);

                                LoadedCollection.Add(new LibraryModel
                                {
                                    AuthorLib = newAuthor,
                                    BookLib = newBook
                                });
                            }
                            else
                            {
                                var checkBookInDB = libColection.Where(b => b.BookLib.BookName == data[4] &&
                                                                       b.BookLib.BookYear == bookYear &&
                                                                       b.BookLib.AuthorId == checkAuthorInDB);

                                if (!checkBookInDB.Any())
                                {
                                    Book newBook = new Book
                                    {
                                        BookName = data[4],
                                        BookYear = bookYear,
                                        AuthorId = checkAuthorInDB
                                    };

                                    _bookRepository.Insert(newBook);

                                    LoadedCollection.Add(new LibraryModel
                                    {
                                        AuthorLib = libColection.Where(a => a.AuthorLib.Id == checkAuthorInDB)
                                                                .Select(a => a.AuthorLib)
                                                                .First(),
                                        BookLib = newBook
                                    });
                                }
                                else
                                {
                                    continue;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            listErrors.Add(new Tuple<string, string>(line, e.Message));
                        }
                    }

                    _bookRepository.Save();

                    if (listErrors.Count == 0 && LoadedCollection.Count == 0)
                    {
                        MessageBox.Show("There are no lines in the file or the provided data from the file is already in the Library.");
                    }
                    else if(listErrors.Count > 0)
                    {
                        FailedRowsFromFile(listErrors);
                    }    
                    else
                    {
                        MessageBox.Show("Data has been uploaded to Library.");
                    }

                    LoadedCollectionView = CollectionViewSource.GetDefaultView(LoadedCollection);
                    OnPropertyChanged("LoadedCollectionView");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                MessageBox.Show(e.InnerException?.Message);
            }
        }

        private void FailedRowsFromFile(List<Tuple<string, string>> listErrors)
        {
            string badRowsCsv = string.Empty;
            string badRowsInfo = string.Empty;

            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Excel files (*.csv)|*.csv";

            int i = 1;

            badRowsInfo += "There are lines with errors in file. You can save these lines to file after closing this window.\n\n";
            
            foreach (var tuple in listErrors)
            {
                badRowsCsv += tuple.Item1 + "\n";
                
                badRowsInfo += $"Line {i}:\n" + tuple.Item1 + "\nError: " + tuple.Item2 + "\n";
                
                i++;
            }

            MessageBox.Show(badRowsInfo);

            if (saveFileDialog.ShowDialog() == true)
            {
                using FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create);
                using StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
                writer.Write(badRowsCsv);
            }
        }


        //--------------------------filter for export data

        private ObservableCollection<LibraryModel> _libraryCollection;
        
        public ObservableCollection<LibraryModel> LibraryCollection
        {
            get { return _libraryCollection; }
            set { _libraryCollection = value; }
        }

        private ICollectionView _libraryCollectionView;

        public ICollectionView LibraryCollectionView
        {
            get { return _libraryCollectionView; }
            set { _libraryCollectionView = value; }
        }

        public ICommand LoadFromDBCommand => new RelayCommand(LoadOrRefreshData);

        private void LoadOrRefreshData(object commandParameter)
        {
            _libraryCollection = GetLibraryCollection();
            LibraryCollectionView = CollectionViewSource.GetDefaultView(LibraryCollection);

            LibraryFilter libFilter = new LibraryFilter();
            libFilter.AddFilter(
                libModel => libModel.AuthorLib.AuthorFirstName.Contains(AuthorFirstNameFilter));

            libFilter.AddFilter(
                libModel => libModel.AuthorLib.AuthorLastName.Contains(AuthorLastNameFilter));

            libFilter.AddFilter(
                libModel => libModel.AuthorLib.AuthorSurName.Contains(AuthorSurNameFilter));
            
            libFilter.AddFilter(
                libModel => string.IsNullOrEmpty(BookYearFilter) || libModel.BookLib.BookYear.ToString().StartsWith(BookYearFilter));

            LibraryCollectionView.Filter = libFilter.Filter;
            OnPropertyChanged("LibraryCollectionView");

            if(LibraryCollectionView == null || LibraryCollectionView?.Cast<LibraryModel>().ToList().Count == 0)
                MessageBox.Show("There is no data in the Library.");
            else
                MessageBox.Show("Data has been loaded/refreshed.");
        }

        private string _authorFirstNameFilter = string.Empty;
        
        public string AuthorFirstNameFilter
        {
            get { return _authorFirstNameFilter; }
            set
            {
                _authorFirstNameFilter = value;
                LibraryCollectionView?.Refresh();
            }
        }

        private string _authorLastNameFilter = string.Empty;
        public string AuthorLastNameFilter
        {
            get { return _authorLastNameFilter; }
            set
            {
                _authorLastNameFilter = value;
                LibraryCollectionView?.Refresh();
            }
        }

        private string _authorSurNameFilter = string.Empty;
        public string AuthorSurNameFilter
        {
            get { return _authorSurNameFilter; }
            set
            {
                _authorSurNameFilter = value;
                LibraryCollectionView?.Refresh();
            }
        }

        private string _bookYearFilter = string.Empty;
        public string BookYearFilter
        {
            get { return _bookYearFilter; }
            set
            {
                _bookYearFilter = value;
                LibraryCollectionView?.Refresh();
            }
        }

        //--------------------------sort collection

        public ICommand SortLoadedCollection => new RelayCommand(SortCollection,
                                                     (obj) => LoadedCollectionView != null &&
                                                     LoadedCollectionView?.Cast<LibraryModel>().ToList().Count > 0);

        public ICommand SortLibraryCollection => new RelayCommand(SortCollection,
                                             (obj) => LibraryCollectionView != null &&
                                             LibraryCollectionView?.Cast<LibraryModel>().ToList().Count > 0);

        bool sortAscending = true;

        void SortCollection(object commandParameter)
        {
            string[] commandString = ((string)commandParameter).Split(';');

            string viewToSort = commandString[0];
            string sortColumn = commandString[1];

            (viewToSort == "LoadedCollectionView" ? LoadedCollectionView : LibraryCollectionView).SortDescriptions.Clear();
 
            if (sortAscending)
            {
                (viewToSort == "LoadedCollectionView" ? LoadedCollectionView : LibraryCollectionView).SortDescriptions.Add
                    (new SortDescription(sortColumn, ListSortDirection.Ascending));
                sortAscending = false;
            }
            else
            {
                (viewToSort == "LoadedCollectionView" ? LoadedCollectionView : LibraryCollectionView).SortDescriptions.Add
                    (new SortDescription(sortColumn, ListSortDirection.Descending));
                sortAscending = true;
            }
        }


        //--------------------------export data

        public string XmlFormat { get; set; } = ".xml";
        public string CsvFormat { get; set; } = ".csv";

        public ICommand ExportCommand => new RelayCommand(ExportDataFromDB, 
                                                          (obj) => LibraryCollectionView != null &&
                                                          LibraryCollectionView?.Cast<LibraryModel>().ToList().Count>0);


        public void ExportDataFromDB(object commandParameter)
        {
            try
            {
                string extension = commandParameter.ToString();

                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.Filter = (extension == ".csv") ? "Excel files (*.csv)|*.csv" : "XML files (*.xml)|*.xml";

                if (saveFileDialog.ShowDialog() == true)
                {
                    string libraryData = "";

                    if (extension == ".csv")
                    {

                        foreach (LibraryModel obj in LibraryCollectionView)
                        {
                            libraryData += obj.ToString();
                            libraryData += '\n';
                        }
                    }
                    else
                    {
                        int id = 1;
                        libraryData = new XElement(
                            "Library",
                            from obj in LibraryCollectionView.Cast<LibraryModel>().ToList()
                            select new XElement("Record",
                                   new XAttribute("id", id++),
                                   new XElement("FirstName", obj.AuthorLib.AuthorFirstName),
                                   new XElement("LastName", obj.AuthorLib.AuthorLastName),
                                   new XElement("SurName", obj.AuthorLib.AuthorSurName),
                                   new XElement("Birthdate", obj.AuthorLib.AuthorBirthDate),
                                   new XElement("BookName", obj.BookLib.BookName),
                                   new XElement("BookYear", obj.BookLib.BookYear)
                        )).ToString();
                    }

                    using FileStream fs = new FileStream(saveFileDialog.FileName, FileMode.Create);
                    using StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
                    writer.Write(libraryData);

                    MessageBox.Show($"Data uploaded to a file: {saveFileDialog.FileName}");
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged([CallerMemberName] string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }
}
