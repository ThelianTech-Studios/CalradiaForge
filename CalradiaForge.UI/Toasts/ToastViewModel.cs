namespace CalradiaForge.UI.Toasts {
	using System;
	using System.ComponentModel;
	using System.Runtime.CompilerServices;
	using System.Windows.Input;
	using System.Windows.Media;

	using MahApps.Metro.IconPacks;

	/// <summary>
	/// Bindable view model for a single toast notification.
	/// Owns its own Id and exposes close/dismiss commands.
	/// </summary>
	public sealed class ToastViewModel : INotifyPropertyChanged {
		private string _title = string.Empty;
		private string _message = string.Empty;
		private string _templateKey = "Default";
		private bool _isPersistent;
		private bool _allowClickDismiss = true;
		private bool _showCloseButton = true;
		private bool _isClosing;
		private bool _isTimerRunning;
		private Brush _accentBrush = Brushes.White;
		private PackIconMaterialKind _iconKind = PackIconMaterialKind.InformationOutline;
		private double _progressValue;
		private double _progressMax = 100;
		private TimeSpan _remainingTime;
		private DateTime _timerStartUtc;

		private readonly Action<Guid> _closeCallback;

		public ToastViewModel(Action<Guid> closeCallback) {
			_closeCallback = closeCallback ?? throw new ArgumentNullException(nameof(closeCallback));
			CloseCommand = new RelayCommand(() => _closeCallback(Id));
			ClickDismissCommand = new RelayCommand(
				() => { if (AllowClickDismiss) { _closeCallback(Id); } });
		}

		/// <summary>Unique identifier for this toast instance.</summary>
		public Guid Id { get; } = Guid.NewGuid();

		public string Title {
			get => _title;
			set { _title = value; OnPropertyChanged(); }
		}

		public string Message {
			get => _message;
			set { _message = value; OnPropertyChanged(); }
		}

		public string TemplateKey {
			get => _templateKey;
			set { _templateKey = value; OnPropertyChanged(); }
		}

		public bool IsPersistent {
			get => _isPersistent;
			set { _isPersistent = value; OnPropertyChanged(); }
		}

		public bool AllowClickDismiss {
			get => _allowClickDismiss;
			set { _allowClickDismiss = value; OnPropertyChanged(); }
		}

		public bool ShowCloseButton {
			get => _showCloseButton;
			set { _showCloseButton = value; OnPropertyChanged(); }
		}

		public bool IsClosing {
			get => _isClosing;
			set { _isClosing = value; OnPropertyChanged(); }
		}

		public bool IsTimerRunning {
			get => _isTimerRunning;
			set { _isTimerRunning = value; OnPropertyChanged(); }
		}

		public Brush AccentBrush {
			get => _accentBrush;
			set { _accentBrush = value; OnPropertyChanged(); }
		}

		public PackIconMaterialKind IconKind {
			get => _iconKind;
			set { _iconKind = value; OnPropertyChanged(); }
		}

		public double ProgressValue {
			get => _progressValue;
			set { _progressValue = value; OnPropertyChanged(); }
		}

		public double ProgressMax {
			get => _progressMax;
			set { _progressMax = value; OnPropertyChanged(); }
		}

		public TimeSpan RemainingTime {
			get => _remainingTime;
			set { _remainingTime = value; OnPropertyChanged(); }
		}

		public DateTime TimerStartUtc {
			get => _timerStartUtc;
			set { _timerStartUtc = value; OnPropertyChanged(); }
		}

		public ICommand CloseCommand { get; }
		public ICommand ClickDismissCommand { get; }

		#region INotifyPropertyChanged
		public event PropertyChangedEventHandler? PropertyChanged;
		private void OnPropertyChanged([CallerMemberName] string name = "") {
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
		}
		#endregion
	}

	/// <summary>
	/// Minimal relay command for toast actions. No external dependency required.
	/// </summary>
	internal sealed class RelayCommand : ICommand {
		private readonly Action _execute;
		private readonly Func<bool>? _canExecute;

		public RelayCommand(Action execute, Func<bool>? canExecute = null) {
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
			_canExecute = canExecute;
		}

		public event EventHandler? CanExecuteChanged {
			add => CommandManager.RequerySuggested += value;
			remove => CommandManager.RequerySuggested -= value;
		}

		public bool CanExecute(object? parameter) => _canExecute?.Invoke() ?? true;
		public void Execute(object? parameter) => _execute();
	}
}