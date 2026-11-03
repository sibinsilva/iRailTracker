using CommunityToolkit.Mvvm.ComponentModel;

namespace iRailTracker.Model
{
    public partial class TrainJourney : ObservableObject
    {
        public string TrainCode { get; set; } = string.Empty;
        /// <summary>Date the train's service started (Irish Rail's Traindate); needed to look up its movements.</summary>
        public DateTime TrainDate { get; set; } = DateTime.Today;
        public string Origin { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string CurrentStatus { get; set; } = string.Empty;
        public string LastLocation { get; set; } = string.Empty;
        public string DueIn { get; set; } = string.Empty;
        /// <summary>Expected time at the selected station: departure, or arrival if the train terminates there.</summary>
        public string ExpectedArrival { get; set; } = string.Empty;
        /// <summary>"Departs" or "Arrives", matching <see cref="ExpectedArrival"/>.</summary>
        public string ExpectedTimeLabel { get; set; } = "Departs";
        public string Late { get; set; } = string.Empty;
        public string LateDisplay { get; set; } = string.Empty;
        public Color LateColor { get; set; } = Color.FromArgb("#DC2626");
        /// <summary>Headline for the card: "Due" or "12 min".</summary>
        public string DueText { get; set; } = string.Empty;
        /// <summary>Minutes until due, for ordering.</summary>
        public int DueMinutes { get; set; }
        /// <summary>Board direction, e.g. "Northbound" or "To Maynooth".</summary>
        public string Direction { get; set; } = string.Empty;
        /// <summary>True for DART services (the board's Traintype is "DART" or "Train").</summary>
        public bool IsDart { get; set; }
        /// <summary>Translucent tint of <see cref="LateColor"/> behind the status badge (works in light and dark).</summary>
        public Color LateBadgeColor { get; set; } = Color.FromArgb("#29DC2626");

#pragma warning disable MVVMTK0045 // Partial-property ObservableProperty isn't generating for this project; field-based is fine since we don't publish Native AOT.
        [ObservableProperty]
        private bool isTracking;
#pragma warning restore MVVMTK0045
    }
}
