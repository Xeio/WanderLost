namespace WanderLost.Client;

public class ClickTrap
{
    public event EventHandler<bool>? HasChanged;

    private bool _hasClicked;

    public bool HasClicked
    {
        get { return _hasClicked; }
        set 
        {
            if (HasClicked != value)
            {
                _hasClicked = value;
                HasChanged?.Invoke(this, value);
            }
        }
    }
}
