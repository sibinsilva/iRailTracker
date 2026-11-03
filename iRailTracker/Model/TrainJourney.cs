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
        public string ExpectedArrival { get; set; } = string.Empty;
        public string Late { get; set; } = string.Empty;
        public string LateDisplay { get; set; } = string.Empty;
        public Color LateColor { get; set; } = Color.FromArgb("#DC2626");

#pragma warning disable MVVMTK0045 // Partial-property ObservableProperty isn't generating for this project; field-based is fine since we don't publish Native AOT.
        [ObservableProperty]
        private bool isTracking;
#pragma warning restore MVVMTK0045
    }
}
