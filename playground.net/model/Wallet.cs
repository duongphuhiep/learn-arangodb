namespace arango_model_csharp
{
    public record class Wallet : Document
    {
        public decimal balance { get; set; }
        public string name { get; set; }
    }
}
