using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Windows.Input;
using TP.ConcurrentProgramming.Presentation.Model;
using TP.ConcurrentProgramming.Presentation.ViewModel.MVVMLight;
using ModelIBall = TP.ConcurrentProgramming.Presentation.Model.IBall;

namespace TP.ConcurrentProgramming.Presentation.ViewModel
{
    public class MainWindowViewModel : ViewModelBase, IDisposable
    {
        // Zmienna przechowująca liczbę kulek
        private int _numberOfBalls = 0;

        // Właściwość NumberOfBalls, która jest bindowana
        public int NumberOfBalls
        {
            get => _numberOfBalls;
            set
            {
                if (_numberOfBalls != value)
                {
                    _numberOfBalls = value;
                    RaisePropertyChanged(nameof(NumberOfBalls)); // Powiadomienie o zmianie
                }
            }
        }

        public MainWindowViewModel() : this(null) { }

        internal MainWindowViewModel(ModelAbstractApi modelLayerAPI)
        {
            ModelLayer = modelLayerAPI == null ? ModelAbstractApi.CreateModel() : modelLayerAPI;
            Observer = ModelLayer.Subscribe<ModelIBall>(x => Balls.Add(x));
        }

        // Komenda Start, która uruchamia ModelLayer z liczbą kulek
        public ICommand StartCommand => new RelayCommand(() => Start(NumberOfBalls));

        // Komenda Stop
        public ICommand StopCommand => new RelayCommand(() => Stop());

        private bool _started = false;
        bool _initialStartIgnored = false;

        public void Start(int numberOfBalls)
        {
            if (!_initialStartIgnored)
            {
                _initialStartIgnored = true;
                return; // ignoruj pierwsze wywołanie
            }

            if (_started) return;

            Debug.WriteLine($"[ViewModel] Number of Balls: {numberOfBalls}");
            ModelLayer.Start(numberOfBalls);
            _started = true;
        }

        public void Stop()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));
            ModelLayer.Stop();
        }

        // Kolekcja kulek
        public ObservableCollection<ModelIBall> Balls { get; } = new ObservableCollection<ModelIBall>();

        public int CurrentBallCount => Balls.Count;

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                if (disposing)
                {
                    Balls.Clear();
                    Observer.Dispose();
                    ModelLayer.Dispose();
                }

                Disposed = true;
            }
        }

        public void Dispose()
        {
            if (Disposed)
                throw new ObjectDisposedException(nameof(MainWindowViewModel));
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        #endregion IDisposable

        #region private

        private IDisposable Observer = null;
        private ModelAbstractApi ModelLayer;
        private bool Disposed = false;

        #endregion private
    }
}
