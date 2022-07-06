using System;
using System.Collections.Generic;
using System.Linq;

namespace LibraryWPF.Utils
{
    internal class LibraryFilter
    {
        private List<Predicate<BookCollectionItem>> _filters;
        public Predicate<object> Filter { get; private set; }

        public LibraryFilter()
        {
            _filters = new List<Predicate<BookCollectionItem>>();
            Filter = InternalFilter;
        }

        private bool InternalFilter(object obj)
        {
            if(_filters.Count == 0)
                return true;

            var model = obj as BookCollectionItem;
            return _filters.Aggregate(true,
                    (prevValue, predicate) => prevValue && predicate(model));
                       
        }

        public void AddFilter(Predicate<BookCollectionItem> filter)
        {
            _filters.Add(filter);
        }
    }
}
