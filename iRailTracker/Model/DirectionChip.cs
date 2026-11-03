using CommunityToolkit.Mvvm.ComponentModel;

namespace iRailTracker.Model
{
    /// <summary>A direction filter chip on Home ("All", "Northbound", "To Maynooth", …).</summary>
    public partial class DirectionChip : ObservableObject
    {
        public const string All = "All directions";

        public DirectionChip(string name) => Name = name;

        public string Name { get; }

#pragma warning disable MVVMTK0045 // field-based ObservableProperty, as in TrainJourney
        [ObservableProperty]
        private bool isSelected;
#pragma warning restore MVVMTK0045
    }
}
