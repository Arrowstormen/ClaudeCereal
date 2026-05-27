namespace ClaudeCereal.Authentication;

public class BasicAuthSettings
{
    public List<BasicAuthUser> Users { get; set; } = [];
}

public class BasicAuthUser
{
    public required string Username { get; set; }
    public required string Password { get; set; }
    public string[] Roles { get; set; } = [];
}
