using System.ComponentModel;
namespace LPGDataAnalyzer.Models.Common
{
    public class SortableBindingList<T> : BindingList<T>
    {
        private bool _isSorted;
        private ListSortDirection _sortDirection;
        private PropertyDescriptor _sortProperty;

        public SortableBindingList() : base() { }
        public SortableBindingList(IList<T> list) : base(list) { }

        protected override bool SupportsSortingCore => true;
        protected override bool IsSortedCore => _isSorted;
        protected override PropertyDescriptor SortPropertyCore => _sortProperty;
        protected override ListSortDirection SortDirectionCore => _sortDirection;

        protected override void ApplySortCore(PropertyDescriptor prop, ListSortDirection direction)
        {
            var itemsList = (List<T>)this.Items;
            var propInfo = typeof(T).GetProperty(prop.Name);

            if (propInfo == null) return;

            itemsList.Sort((a, b) =>
            {
                var valA = propInfo.GetValue(a);
                var valB = propInfo.GetValue(b);
                int result = Comparer<object>.Default.Compare(valA, valB);
                return direction == ListSortDirection.Ascending ? result : -result;
            });

            _sortProperty = prop;
            _sortDirection = direction;
            _isSorted = true;

            this.OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
        }

        protected override void RemoveSortCore()
        {
            _isSorted = false;
        }
    }
}