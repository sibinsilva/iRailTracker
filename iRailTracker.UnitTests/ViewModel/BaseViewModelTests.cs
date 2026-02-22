using iRailTracker.ViewModel;
using System.ComponentModel;

namespace iRailTracker.UnitTests.ViewModel;

public class BaseViewModelTests
{
    private class TestableBaseViewModel : BaseViewModel
    {
        private string _testProperty = string.Empty;
        public string TestProperty
        {
            get => _testProperty;
            set => SetProperty(ref _testProperty, value);
        }

        public bool InvokeSetProperty<T>(ref T backingStore, T value, string propertyName)
            => SetProperty(ref backingStore, value, propertyName);

        public new void OnPropertyChanged(string propertyName = "")
            => base.OnPropertyChanged(propertyName);

        public new void ShowError(string message)
            => base.ShowError(message);
    }

    [Fact]
    public void SetProperty_SameValue_ReturnsFalse()
    {
        var vm = new TestableBaseViewModel();
        vm.TestProperty = "test";

        string backing = "test";
        var result = vm.InvokeSetProperty(ref backing, "test", "TestProperty");

        Assert.False(result);
    }

    [Fact]
    public void SetProperty_SameValue_DoesNotRaisePropertyChanged()
    {
        var vm = new TestableBaseViewModel();
        vm.TestProperty = "test";

        var raised = false;
        vm.PropertyChanged += (s, e) => raised = true;

        vm.TestProperty = "test";

        Assert.False(raised);
    }

    [Fact]
    public void SetProperty_DifferentValue_ReturnsTrue()
    {
        var vm = new TestableBaseViewModel();

        string backing = "old";
        var result = vm.InvokeSetProperty(ref backing, "new", "TestProperty");

        Assert.True(result);
    }

    [Fact]
    public void SetProperty_DifferentValue_UpdatesBackingField()
    {
        var vm = new TestableBaseViewModel();

        vm.TestProperty = "newValue";

        Assert.Equal("newValue", vm.TestProperty);
    }

    [Fact]
    public void SetProperty_DifferentValue_RaisesPropertyChanged()
    {
        var vm = new TestableBaseViewModel();

        string? changedProperty = null;
        vm.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        vm.TestProperty = "newValue";

        Assert.Equal("TestProperty", changedProperty);
    }

    [Fact]
    public void OnPropertyChanged_RaisesPropertyChangedEvent()
    {
        var vm = new TestableBaseViewModel();

        string? changedProperty = null;
        vm.PropertyChanged += (s, e) => changedProperty = e.PropertyName;

        vm.OnPropertyChanged("SomeProperty");

        Assert.Equal("SomeProperty", changedProperty);
    }

    [Fact]
    public void ShowError_RaisesErrorOccurredEvent()
    {
        var vm = new TestableBaseViewModel();

        string? errorMessage = null;
        vm.ErrorOccurred += (s, e) => errorMessage = e;

        vm.ShowError("test error");

        Assert.Equal("test error", errorMessage);
    }

    [Fact]
    public void ShowError_WithNoSubscribers_DoesNotThrow()
    {
        var vm = new TestableBaseViewModel();

        var exception = Record.Exception(() => vm.ShowError("test error"));

        Assert.Null(exception);
    }
}
