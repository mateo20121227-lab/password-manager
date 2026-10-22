namespace PasswordManager
{
    public class PasswordEntry
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Site { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime UpdatedAt { get; set; } = DateTime.Now;
    }
}