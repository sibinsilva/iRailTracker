using CommunityToolkit.Mvvm.ComponentModel;

namespace iRailTracker.Model
{
    /// <summary>A day toggle (M T W T F S S) for the commute preset.</summary>
    public partial class WeekdayChip : ObservableObject
    {
        public WeekdayChip(DayOfWeek day, bool selected)
        {
            Day = day;
            Label = day.ToString()[..1];
            isSelected = selected;
        }

        public DayOfWeek Day { get; }
        public string Label { get; }

#pragma warning disable MVVMTK0045 // field-based ObservableProperty, as in TrainJourney
        [ObservableProperty]
        private bool isSelected;
#pragma warning restore MVVMTK0045
    }
}
