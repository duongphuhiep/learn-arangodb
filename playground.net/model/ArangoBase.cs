namespace arango_model_csharp
{
    public record Document
    {
        public string? _key { get; set; }
        public string _rev { get; set; }
    }

    public record Edge : Document
    {
        public required string _from { get; set; }
        public required string _to { get; set; }
    }
}
