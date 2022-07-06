using LibraryWPF.Commands;
using LibraryWPF.Models;
using LibraryWPF.Repositories;
using LibraryWPF.Utils;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
            LibraryCollectionViewRefresh();
        }

        public ObservableCollection<BookCollectionItem> GetLibraryCollection() 
        {
            var authorCollection = _authorRepository.GetObjectCollection();
            var bookCollection = _bookRepository.GetObjectCollection();

            return  new ObservableCollection<BookCollectionItem>(
                        from authors in authorCollection
                        join books in bookCollection on authors.Id equals books.AuthorId
                        select new BookCollectionItem
                        {
                            Author = new Author
                            {
                                Id = authors.Id,
                                FirstName = authors.FirstName,
                                LastName = authors.LastName,
                                SurName = authors.SurName,
                                BirthDate = authors.BirthDate
                            },
                            Book = new Book
                            {
                                Name = books.Name,
                                Year = books.Year,
                                AuthorId = books.AuthorId
                            }
                        }
                    );
            
        }

        public void LibraryCollectionViewRefresh()
        {
            _libraryCollection = GetLibraryCollection();
            LibraryCollectionView = CollectionViewSource.GetDefaultView(LibraryCollection);

            LibraryFilter libFilter = new LibraryFilter();
            libFilter.AddFilter(
                libModel => libModel.Author.FirstName.ToUpper().StartsWith(AuthorFirstNameFilter.ToUpper()));

            libFilter.AddFilter(
                libModel => libModel.Author.LastName.ToUpper().StartsWith(AuthorLastNameFilter.ToUpper()));

            libFilter.AddFilter(
                libModel => libModel.Author.SurName.ToUpper().StartsWith(AuthorSurNameFilter.ToUpper()));

            libFilter.AddFilter(
                libModel => string.IsNullOrEmpty(BookYearFilter) || libModel.Book.Year.ToString().StartsWith(BookYearFilter));

            LibraryCollectionView.Filter = libFilter.Filter;

            OnPropertyChanged("LibraryCollectionView");

        }
        //--------------------------open file/import data

        private ObservableCollection<BookCollectionItem> _loadedCollection;
        public ObservableCollection<BookCollectionItem> LoadedCollection
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
            LoadedCollection = new ObservableCollection<BookCollectionItem>();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv";

            string filePath = string.Empty;

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    List<IncorrectLines> listErrors = new List<IncorrectLines>();

                    List<string> uniqueLines = new List<string>();

                    //remove duplicates in file
                    using (StreamReader streamReader = File.OpenText(openFileDialog.FileName))
                    {
                        filePath = openFileDialog.FileName;
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

                            DateTime authorBirthDate;

                            if(!DateTime.TryParse(data[3], out authorBirthDate))
                            {
                                var formatStrings = new string[] { "MM/dd/yyyy", "yyyy-MM-dd", "dd/MM/yyyy" };
                                if (!DateTime.TryParseExact(data[3], formatStrings, CultureInfo.InvariantCulture, DateTimeStyles.None, out authorBirthDate))
                                {
                                    listErrors.Add(new IncorrectLines { Line = line, Error = "Incorrect date format for parsing." });
                                    continue;
                                }
                            }


                            if (data.Length != 6)
                            {
                                listErrors.Add(new IncorrectLines{ Line = line, Error = "Incorrect line format for parsing." });
                                continue;
                            }

                            if (!data[0].All(Char.IsLetter) || !data[1].All(Char.IsLetter) || !data[2].All(Char.IsLetter) ||
                                string.IsNullOrEmpty(data[0]) || string.IsNullOrEmpty(data[1]))
                            {
                                listErrors.Add(new IncorrectLines { Line = line, Error = "Incorrect string format for parsing first name, last name or surname." });
                                continue;
                            }

                            if (string.IsNullOrEmpty(data[4]))
                            {
                                listErrors.Add(new IncorrectLines { Line = line, Error = "Book name can't be empty." });
                                continue;
                            }

                            int bookYear = 0;
                            if (!string.IsNullOrEmpty(data[5]))
                            {
                                if (!int.TryParse(data[5], out bookYear) || bookYear > DateTime.Now.Date.Year || bookYear < 1)
                                {
                                    listErrors.Add(new IncorrectLines { Line = line, Error = "Incorrect book year." });
                                    continue;
                                }
                            }

                            authorBirthDate = authorBirthDate.Date;

                            var checkAuthorIdInDB = libColection.Where(a => a.Author.FirstName == data[0].Trim() && a.Author.LastName == data[1].Trim() &&
                                                                          a.Author.SurName == data[2].Trim() && a.Author.BirthDate == authorBirthDate)
                                                                .Select(aid => aid.Author.Id)
                                                                .FirstOrDefault();

                            var checkAuthorInLoadedCollection = LoadedCollection.Where(a => a.Author.FirstName == data[0].Trim() && a.Author.LastName == data[1].Trim() &&
                                                                                       a.Author.SurName == data[2].Trim() && a.Author.BirthDate == authorBirthDate)
                                                                                .Select(aid => aid.Author)
                                                                                .FirstOrDefault();

                            if (checkAuthorIdInDB == 0 && checkAuthorInLoadedCollection is null)
                            {
                                //No author in db and no author in collection inside the file data

                                Author newAuthor = new Author
                                {
                                    FirstName = data[0].Trim(),
                                    LastName = data[1].Trim(),
                                    SurName = data[2].Trim(),
                                    BirthDate = authorBirthDate,
                                };

                                Book newBook = new Book
                                {
                                    Name = data[4].Trim(),
                                    Year = bookYear,
                                    Author = newAuthor
                                };

                                _authorRepository.Insert(newAuthor);
                                _bookRepository.Insert(newBook);

                                LoadedCollection.Add(new BookCollectionItem
                                {
                                    Author = newAuthor,
                                    Book = newBook
                                });
                            }
                            else if (checkAuthorIdInDB == 0 && checkAuthorInLoadedCollection is not null)
                            {
                                //No Author in db but there is author in data from file

                                Book newBookForExistingAuthorInLoadedCollection = new Book
                                {
                                    Name = data[4].Trim(),
                                    Year = bookYear,
                                    Author = checkAuthorInLoadedCollection
                                };

                                _bookRepository.Insert(newBookForExistingAuthorInLoadedCollection);

                                LoadedCollection.Add(new BookCollectionItem
                                {
                                    Author = checkAuthorInLoadedCollection,

                                    Book = newBookForExistingAuthorInLoadedCollection
                                });
                            }
                            else 
                            { 
                                var checkBookInDB = libColection.Where(b => b.Book.Name == data[4].Trim() &&
                                                                       b.Book.Year == bookYear &&
                                                                       b.Book.AuthorId == checkAuthorIdInDB);
                                
                                if (!checkBookInDB.Any())
                                {
                                    // There is author in db, but there is no book for author in db with data provided

                                    Book newBookForExistingAuthorInDb = new Book
                                    {
                                        Name = data[4].Trim(),
                                        Year = bookYear,
                                        AuthorId = checkAuthorIdInDB
                                    };

                                    _bookRepository.Insert(newBookForExistingAuthorInDb);

                                    LoadedCollection.Add(new BookCollectionItem
                                    {
                                        Author = libColection.Where(a => a.Author.Id == checkAuthorIdInDB)
                                                                .Select(a => a.Author)
                                                                .First(),

                                        Book = newBookForExistingAuthorInDb
                                    });
                                }
                                else
                                {
                                    //There is author and book in db

                                    continue;
                                }
                            }

                        }
                        catch (Exception e)
                        {
                            listErrors.Add(new IncorrectLines { Line = line, Error = e.Message });
                        }
                    }

                    _bookRepository.Save();

                    if (listErrors.Count == 0 && LoadedCollection.Count == 0)
                    {
                        MessageBox.Show("There are no lines in the file or the provided data from the file is already in the Library.");
                    }
                    else if(listErrors.Count > 0)
                    {
                        FailedRowsFromFile(listErrors, filePath);
                    }    
                    else
                    {
                        MessageBox.Show("Data has been uploaded to Library.");
                    }

                    LoadedCollectionView = CollectionViewSource.GetDefaultView(LoadedCollection);
                    OnPropertyChanged("LoadedCollectionView");

                    LibraryCollectionViewRefresh();

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                if(e.InnerException is not null)
                    MessageBox.Show(e.InnerException.Message);
            }
        }

        private void FailedRowsFromFile(List<IncorrectLines> listErrors, string filePath)
        {
            string badRowsCsv = string.Empty;
            string badRowsInfo = string.Empty;

            int i = 1;
            
            foreach (var incorrectLine in listErrors)
            {
                badRowsCsv += incorrectLine.Line + "\n";
                
                badRowsInfo += $"Line {i}:\n" + incorrectLine.Line + "\nError: " + incorrectLine.Error + "\n";
                
                i++;
            }

            Dictionary<string, string> files = new Dictionary<string, string>
            {
                { filePath.Substring(0, filePath.Length - 4) + "_badrows.csv", badRowsCsv },
                { filePath.Substring(0, filePath.Length - 4) + "_badrowsinfo.txt", badRowsInfo}
            };

            string message = "There are lines with errors in file. Check the files below for more info:\n";

            foreach (var file in files)
            {
                using FileStream fs = new FileStream(file.Key, FileMode.Create);
                using StreamWriter writer = new StreamWriter(fs, Encoding.UTF8);
                writer.Write(file.Value);

                message += "\n" + file.Key;
            }

            MessageBox.Show(message);
        }

        //--------------------------filter for export data

        private ObservableCollection<BookCollectionItem> _libraryCollection;
        
        public ObservableCollection<BookCollectionItem> LibraryCollection
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

            LibraryCollectionViewRefresh();

            AuthorFirstNameFilter = string.Empty;
            OnPropertyChanged("AuthorFirstNameFilter");
            AuthorLastNameFilter = string.Empty;
            OnPropertyChanged("AuthorLastNameFilter");
            AuthorSurNameFilter = string.Empty;
            OnPropertyChanged("AuthorSurNameFilter");
            BookYearFilter = string.Empty;
            OnPropertyChanged("BookYearFilter");

            if(LibraryCollectionView == null || LibraryCollectionView?.Cast<BookCollectionItem>().ToList().Count == 0)
                MessageBox.Show("There is no data in the Library.");
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
                                                     LoadedCollectionView?.Cast<BookCollectionItem>().ToList().Count > 0);

        public ICommand SortLibraryCollection => new RelayCommand(SortCollection,
                                             (obj) => LibraryCollectionView != null &&
                                             LibraryCollectionView?.Cast<BookCollectionItem>().ToList().Count > 0);

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
                                                          LibraryCollectionView?.Cast<BookCollectionItem>().ToList().Count>0);


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

                        foreach (BookCollectionItem obj in LibraryCollectionView)
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
                            from obj in LibraryCollectionView.Cast<BookCollectionItem>().ToList()
                            select new XElement("Record",
                                   new XAttribute("id", id++),
                                   new XElement("FirstName", obj.Author.FirstName),
                                   new XElement("LastName", obj.Author.LastName),
                                   new XElement("SurName", obj.Author.SurName),
                                   new XElement("Birthdate", obj.Author.BirthDate),
                                   new XElement("BookName", obj.Book.Name),
                                   new XElement("BookYear", obj.Book.Year)
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
