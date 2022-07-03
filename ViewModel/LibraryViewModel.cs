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
        private readonly LibraryRepository _libraryRepository;

        public LibraryViewModel()
        {
            _libraryRepository = new LibraryRepository();

        }

        //--------------------------open file

        public IEnumerable<LibraryModel> loadedList;
        public IEnumerable<LibraryModel> LoadedList
        {
            get { return loadedList; } 
            
            set
            {
                loadedList = value;
                OnPropertyChanged();
            }
        }
        
        public ICommand LoadCommand => new RelayCommand(ReadFile);

        private void ReadFile(object commandParameter)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "CSV files (*.csv)|*.csv";

            try
            {
                if (openFileDialog.ShowDialog() == true)
                {
                    string[] lines = File.ReadAllLines(openFileDialog.FileName);

                    LoadedList = lines.Select(line =>
                    {
                        string[] data = line.Split(';');

                        if (data.Length != 6)
                            throw new Exception($"Incorrect line format for parsing.\n\nLine: {line}");

                        if (!data[0].All(Char.IsLetter) || !data[1].All(Char.IsLetter) || !data[2].All(Char.IsLetter) ||
                            string.IsNullOrEmpty(data[0]) || string.IsNullOrEmpty(data[1]) || string.IsNullOrEmpty(data[2]))
                            throw new Exception($"Incorrect string format for parsing first name, last name or surname.\n\nLine: {line}");

                        if (string.IsNullOrEmpty(data[4]))
                            throw new Exception($"Book name can't be empty.\n\nLine: {line}");

                        int bookYear;

                        if (!int.TryParse(data[5], out bookYear) || bookYear > DateTime.Now.Date.Year || bookYear < 1)
                            throw new Exception($"Incorrect book year.\n\nLine: {line}");


                        return new LibraryModel()
                        {
                            AuthorFirstName = data[0],
                            AuthorLastName = data[1],
                            AuthorSurName = data[2],
                            AuthorBirthDate = DateTime.Parse(data[3]).Date,
                            BookName = data[4],
                            BookYear = int.Parse(data[5])
                        };
                        

                    });

                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
                //clear list if error appeared
                LoadedList = null;
            }


        }

        //--------------------------import data

        public ICommand ImportCommand => new RelayCommand(ImportFile, (obj) => loadedList != null);

        public void ImportFile(object commandParameter)
        {
            try
            {
                foreach (LibraryModel obj in loadedList)
                {
                    _libraryRepository.Insert(obj);
                }
                
                _libraryRepository.Save();

                MessageBox.Show("Data has been imported.");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.InnerException.Message);
            }
        }

        //--------------------------filter for export data

        public ObservableCollection<LibraryModel> _libraryCollection;
        
        public ICommand LoadFromDBCommand => new RelayCommand(LoadOrRefreshData);

        private void LoadOrRefreshData(object commandParameter)
        {
            _libraryCollection = _libraryRepository.GetObjectCollection();
            LibraryCollectionView = CollectionViewSource.GetDefaultView(LibraryCollection);

            LibraryFilter libFilter = new LibraryFilter();
            libFilter.AddFilter(
                libModel => libModel.AuthorFirstName.Contains(AuthorFirstNameFilter));

            libFilter.AddFilter(
                libModel => libModel.AuthorLastName.Contains(AuthorLastNameFilter));

            libFilter.AddFilter(
                libModel => libModel.AuthorSurName.Contains(AuthorSurNameFilter));
            
            libFilter.AddFilter(
                libModel => string.IsNullOrEmpty(BookYearFilter) || libModel.BookYear.ToString().Equals(BookYearFilter));

            LibraryCollectionView.Filter = libFilter.Filter;
            OnPropertyChanged("LibraryCollectionView");

            MessageBox.Show("Data has been loaded.");
        }


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

        //--------------------------export data

        public string XmlFormat { get; set; } = ".xml";
        public string CsvFormat { get; set; } = ".csv";

        public ICommand ExportCommand => new RelayCommand(ExportDataFromDB, (obj) => LibraryCollectionView != null);


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
                        libraryData = new XElement(
                            "Library",
                            from obj in LibraryCollectionView.Cast<LibraryModel>().ToList()
                            select new XElement("Record",
                                   new XAttribute("id", obj.Id),
                                   new XElement("FirstName", obj.AuthorFirstName),
                                   new XElement("LastName", obj.AuthorLastName),
                                   new XElement("SurName", obj.AuthorSurName),
                                   new XElement("Birthdate", obj.AuthorBirthDate),
                                   new XElement("BookName", obj.BookName),
                                   new XElement("BookYear", obj.BookYear)
                        )).ToString();

                        
                    }

                    File.WriteAllText(saveFileDialog.FileName, libraryData, Encoding.UTF8);

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
