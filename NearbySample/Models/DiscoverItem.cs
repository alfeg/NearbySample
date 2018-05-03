namespace NearbySample.Models
{
    public class DiscoverItem
    {
        public enum ConnectionState
        {
            Found, Connected, Connecting
        }

        public string Endpoint { get; set; }
        public string Name { get; set; }
        public ConnectionState Status { get; set; }

        public override string ToString() => $"{Name}";
    }
}