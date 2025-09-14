namespace MinecraftRcon
{
    public enum MessageType
    {
        Response = 0,
        _ = 1,
        Command = 2,
        AuthResponse = 2,  // Note: Same value as Command per protocol docs
        Authenticate = 3,
    }
}