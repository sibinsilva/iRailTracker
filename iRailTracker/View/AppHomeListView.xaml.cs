using iRailTracker.Model;

namespace iRailTracker.View;

public partial class AppHomeListView : ContentView
{
	public AppHomeListView()
	{
		InitializeComponent();
	}

	public void ScrollTo(TrainJourney journey)
	{
		JourneysCollectionView.ScrollTo(journey, position: ScrollToPosition.MakeVisible, animate: true);
	}
}
