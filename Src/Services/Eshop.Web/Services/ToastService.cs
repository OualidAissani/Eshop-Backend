namespace Eshop.Web.Services;

public enum ToastType
{
    Success,
    Error,
    Warning,
    Info
}

public class ToastMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Message { get; set; } = string.Empty;
    public ToastType Type { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.Now;
}

public class ToastService
{
    private readonly List<ToastMessage> _toasts = new();
    public event Action? OnChange;

    public IReadOnlyList<ToastMessage> Toasts => _toasts.AsReadOnly();

    public void ShowSuccess(string message)
    {
  Show(message, ToastType.Success);
    }

  public void ShowError(string message)
    {
        Show(message, ToastType.Error);
    }

    public void ShowWarning(string message)
    {
        Show(message, ToastType.Warning);
    }

    public void ShowInfo(string message)
    {
  Show(message, ToastType.Info);
    }

    private void Show(string message, ToastType type)
    {
    var toast = new ToastMessage
   {
    Message = message,
      Type = type
        };

        _toasts.Add(toast);
        OnChange?.Invoke();

        // Auto-remove after 5 seconds
        _ = Task.Delay(5000).ContinueWith(_ =>
     {
       Remove(toast.Id);
   });
    }

    public void Remove(Guid id)
    {
        var toast = _toasts.FirstOrDefault(t => t.Id == id);
 if (toast != null)
        {
     _toasts.Remove(toast);
            OnChange?.Invoke();
        }
    }

    public void Clear()
    {
  _toasts.Clear();
        OnChange?.Invoke();
    }
}
