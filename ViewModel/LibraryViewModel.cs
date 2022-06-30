using Aspose.Cells;
using LibraryWPF.Commands;
using LibraryWPF.Models;
using LibraryWPF.Repositories;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using System.Xml.Linq;


namespace LibraryWPF.ViewModel
{
    public class LibraryViewModel : INotifyPropertyChanged
    {
        private readonly LibraryRepository _libraryRepository;

        public ObservableCollection<LibraryModel> LibraryCollection { get; set; }

        public LibraryViewModel()
        {
            _libraryRepository = new LibraryRepository();
            
        }


        //open file

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
                        // We return a person with the data in order.
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
                    //return;
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message);
            }


        }

        //import data

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

                MessageBox.Show("Data has been loaded.");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.InnerException.Message);
            }
        }


        // export data

        public string XmlFormat { get; set; } = ".xml";
        public string XlsxFormat { get; set; } = ".xlsx";

        public ICommand ExportCommand => new RelayCommand(ExportDataFromDB);

        public void ExportDataFromDB(object commandParameter)
        {
            try
            {
                
                string extension = commandParameter.ToString();
                LibraryCollection = _libraryRepository.GetObjectCollection();
                SaveFileDialog saveFileDialog = new SaveFileDialog();

                saveFileDialog.Filter = (extension == ".xlsx") ? "Excel files (*.xlsx)|*.xlsx" : "XML files (*.xml)|*.xml";


                if (saveFileDialog.ShowDialog() == true)
                {
                    if (extension == ".xlsx")
                    {
                        Workbook wb = new Workbook();
                        Worksheet sheet = wb.Worksheets[0];

                        string cellChars = "ABCDEF";
                        string[] libColumns = new[] { "FirstName", "LastName", "SurName", "Birthdate", "BookName", "BookYear" };
                        
                        for (int i=0; i<cellChars.Length;i++)
                        {
                            sheet.Cells[cellChars[i] + "1"].PutValue(libColumns[i]);
                        }

                        int intIndex = 2;
                        
                        foreach(LibraryModel obj in LibraryCollection)
                        {
                            sheet.Cells[cellChars[0] + intIndex.ToString()].PutValue(obj.AuthorFirstName);
                            sheet.Cells[cellChars[1] + intIndex.ToString()].PutValue(obj.AuthorLastName);
                            sheet.Cells[cellChars[2] + intIndex.ToString()].PutValue(obj.AuthorSurName);
                            sheet.Cells[cellChars[3] + intIndex.ToString()].PutValue(obj.AuthorBirthDate);
                            sheet.Cells[cellChars[4] + intIndex.ToString()].PutValue(obj.BookName);
                            sheet.Cells[cellChars[5] + intIndex.ToString()].PutValue(obj.BookYear);

                            intIndex++;
                        }

                        wb.Save(saveFileDialog.FileName, SaveFormat.Xlsx);
                    }
                    else
                    {
                        string libraryData = new XElement(
                            "Library",
                            from obj in LibraryCollection
                            select new XElement("Record",
                                   new XAttribute("id", obj.Id),
                                   new XElement("FirstName", obj.AuthorFirstName),
                                   new XElement("LastName", obj.AuthorLastName),
                                   new XElement("SurName", obj.AuthorSurName),
                                   new XElement("Birthdate", obj.AuthorBirthDate),
                                   new XElement("BookName", obj.BookName),
                                   new XElement("BookYear", obj.BookYear)
                        )).ToString();

                        File.WriteAllText(saveFileDialog.FileName, libraryData);
                    }

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
