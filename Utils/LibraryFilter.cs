using LibraryWPF.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryWPF.Utils
{
    internal class LibraryFilter
    {
        private List<Predicate<LibraryModel>> _filters;
        public Predicate<object> Filter { get; private set; }

        public LibraryFilter()
        {
            _filters = new List<Predicate<LibraryModel>>();
            Filter = InternalFilter;
        }
        private bool InternalFilter(object obj)
        {
            if(_filters.Count == 0)
                return true;

            var model = obj as LibraryModel;
            return _filters.Aggregate(true,
                    (prevValue, predicate) => prevValue && predicate(model));
                       
        }

        public void AddFilter(Predicate<LibraryModel> filter)
        {
            _filters.Add(filter);
        }
    }
}
