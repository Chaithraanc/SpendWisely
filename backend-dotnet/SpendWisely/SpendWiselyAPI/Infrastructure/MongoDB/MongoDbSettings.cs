namespace SpendWiselyAPI.Infrastructure.MongoDB
{
    public class MongoDbSettings
    {
        public string ConnectionString { get; set; }
        public string DatabaseName { get; set; }
        public string EventsCollectionName { get; set; }
    }
}
