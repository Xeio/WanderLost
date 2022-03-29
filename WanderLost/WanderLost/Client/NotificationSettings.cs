namespace WanderLost.Client
{
    public class NotificationSettings
    {
        public NotificationSettings()
        {
            CardList = new List<string>();
        }
        public List<string> CardList { get; set; }
        public bool NotifyLegendaryRapport { get; set; }
    }
}
