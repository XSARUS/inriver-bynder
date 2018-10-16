namespace Bynder.Api.Model
{
    public class Metaproperty
    {
        public string Id { get; private set; }
        public string Value { get; set; }

        public Metaproperty(string id, string value)
        {
            Id = id;
            Value = value;
        }
    }
}
