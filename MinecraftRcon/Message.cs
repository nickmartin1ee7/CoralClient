namespace MinecraftRcon
{
    public readonly struct Message
    {
        public int Length { get; }
        public int Id { get; }
        public MessageType Type { get; }
        public string Body { get; }

        public Message(int length, int id, MessageType type, string body)
        {
            Length = length;
            Id = id;
            Type = type;
            Body = body;
        }
    }
}