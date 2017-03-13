using System.Collections.Generic;

namespace WpfServerAdmin.ViewModels
{
    public class ListPopupWindowViewModel
    {
        public List<LogEntryViewModel> LogInfo { get; set; }

        public ListPopupWindowViewModel()
        {
            LogInfo = new List<LogEntryViewModel>();
        }
    }
}