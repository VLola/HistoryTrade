namespace HistoryTrade.Model
{
    public class User
    {
        public string UserName { get; set; }
        public string ApiId { get; set; }
        public string ApiHash { get; set; }
        public string PhoneNumber { get; set; }
        public long ChatId { get; set; }
        public override string ToString()
        {
            return UserName;
        }
    }
}
