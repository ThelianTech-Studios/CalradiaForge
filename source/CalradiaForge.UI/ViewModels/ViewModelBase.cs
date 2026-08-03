namespace CalradiaForge.UI.ViewModels;

using CalradiaForge.UI.Lifecycle;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// Minimal retained-ViewModel foundation with explicit, awaitable lifecycle.
/// </summary>
public abstract class ViewModelBase : ObservableObject, IViewModelLifecycle {
	private readonly SemaphoreSlim _initializationGate = new(1, 1);
	private readonly SemaphoreSlim _activationGate = new(1, 1);
	private bool _isInitialized;
	private bool _isActive;

	public bool IsInitialized {
		get => _isInitialized;
		private set => SetProperty(ref _isInitialized, value);
	}

	public bool IsActive {
		get => _isActive;
		private set => SetProperty(ref _isActive, value);
	}

	public async Task InitializeAsync(CancellationToken cancellationToken = default) {
		if (IsInitialized) {
			return;
		}

		await _initializationGate.WaitAsync(cancellationToken);
		try {
			if (IsInitialized) {
				return;
			}

			await OnInitializeAsync(cancellationToken);
			IsInitialized = true;
		} finally {
			_initializationGate.Release();
		}
	}

	public async Task ActivateAsync(CancellationToken cancellationToken = default) {
		await InitializeAsync(cancellationToken);
		await _activationGate.WaitAsync(cancellationToken);
		try {
			await OnActivateAsync(cancellationToken);
			IsActive = true;
		} finally {
			_activationGate.Release();
		}
	}

	public async Task DeactivateAsync(CancellationToken cancellationToken = default) {
		await _activationGate.WaitAsync(cancellationToken);
		try {
			await OnDeactivateAsync(cancellationToken);
			IsActive = false;
		} finally {
			_activationGate.Release();
		}
	}

	protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) =>
		Task.CompletedTask;

	protected virtual Task OnActivateAsync(CancellationToken cancellationToken) =>
		Task.CompletedTask;

	protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken) =>
		Task.CompletedTask;
}
