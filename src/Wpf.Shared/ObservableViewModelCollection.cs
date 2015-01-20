using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Linq;
using System.Windows.Threading;

namespace Wpf.Shared
{
    /// <summary>
    /// Class to have a synchronized list of models and viewmodels
    /// </summary>
    public class ObservableViewModelCollection<TViewModel, TModel> : ObservableCollection<TViewModel>
    {
        private readonly Dispatcher _dispatcher;
        private readonly ObservableCollection<TModel> _source;
        private readonly Func<TModel, TViewModel> _viewModelFactory;

        public ObservableViewModelCollection(Dispatcher dispatcher, ObservableCollection<TModel> source,
            Func<TModel, TViewModel> viewModelFactory)
            : base(source.Select(viewModelFactory))
        {
            _dispatcher = dispatcher;
            _source = source;
            _viewModelFactory = viewModelFactory;
            _source.CollectionChanged += OnSourceCollectionChanged;
        }

        protected virtual TViewModel CreateViewModel(TModel model)
        {
            return _viewModelFactory(model);
        }

        private void OnSourceCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (!_dispatcher.CheckAccess())
            {
                _dispatcher.Invoke(new Action<object, NotifyCollectionChangedEventArgs>(OnSourceCollectionChanged), sender, e);
                return;
            }

            using (BlockReentrancy())
            {
                switch (e.Action)
                {
                    case NotifyCollectionChangedAction.Add:
                        for (var i = 0; i < e.NewItems.Count; i++)
                        {
                            Insert(e.NewStartingIndex + i, CreateViewModel((TModel)e.NewItems[i]));
                        }
                        break;
                    case NotifyCollectionChangedAction.Move:
                        {
                            var oldIndex = e.OldStartingIndex;
                            var newIndex = e.NewStartingIndex;
                            if (e.OldItems.Count == 1)
                            {
                                Move(oldIndex, newIndex);
                            }
                            else
                            {
                                var items = this.Skip(oldIndex).Take(e.OldItems.Count).ToList();
                                for (var i = 0; i < e.OldItems.Count; i++)
                                {
                                    RemoveAt(oldIndex);
                                }
                                if (newIndex > oldIndex)
                                {
                                    newIndex -= e.OldItems.Count;
                                }
                                for (var i = 0; i < items.Count; i++)
                                {
                                    Insert(newIndex + i, items[i]);
                                }
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Remove:
                        {
                            for (var i = 0; i < e.OldItems.Count; i++)
                            {
                                RemoveAt(e.OldStartingIndex);
                            }
                        }
                        break;
                    case NotifyCollectionChangedAction.Replace:
                        // remove
                        for (var i = 0; i < e.OldItems.Count; i++)
                            RemoveAt(e.OldStartingIndex);
                        // add
                        goto case NotifyCollectionChangedAction.Add;
                    case NotifyCollectionChangedAction.Reset:
                        Clear();
                        foreach (var t in e.NewItems)
                            Add(CreateViewModel((TModel)t));
                        break;
                }
            }
        }
    }
}
