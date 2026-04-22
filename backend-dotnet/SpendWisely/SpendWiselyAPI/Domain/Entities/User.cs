namespace SpendWiselyAPI.Domain.Entities
{
    public class User
    {
        public Guid Id { get; private set; }            // Domain-generated GUID
        public string Name { get; private set; }
        public string Email { get; private set; }
        public string PasswordHash { get; private set; }
        public string Role { get; private set; }        // "User" or "Admin"
        public DateTime CreatedAt { get; private set; }

        public DateTime? UpdatedAt { get; private set; }

        public string? RefreshToken { get; private set; }
        public DateTime? RefreshTokenExpiry { get; private set; }

        // Constructor for creating new user
        public User(string name, string email, string passwordHash, string role = "User")
        {
            Id = Guid.NewGuid();
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            Role = role;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = null;
        }
        // Constructor for rehydrating user from database
        public User( Guid id , string name, string email, string passwordHash, string role , DateTime createdAt ,DateTime? updatedAt)
        {
            Id = id;
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Email = email ?? throw new ArgumentNullException(nameof(email));
            PasswordHash = passwordHash ?? throw new ArgumentNullException(nameof(passwordHash));
            Role = role;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;

        }

        // Methods to update user details
        public void UpdateName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name cannot be empty", nameof(name));

            Name = name;
        }

        public void UpdateEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new ArgumentException("Email cannot be empty", nameof(email));

            Email = email;
        }

        public void UpdatePassword(string passwordHash)
        {
            if (string.IsNullOrWhiteSpace(passwordHash))
                throw new ArgumentException("PasswordHash cannot be empty", nameof(passwordHash));

            PasswordHash = passwordHash;
        }

        public void SetRole(string role)
        {
            if (string.IsNullOrWhiteSpace(role))
                throw new ArgumentException("Role cannot be empty", nameof(role));

            Role = role;
        }

        public void SetRefreshToken(string token, DateTime expiry)
        {
            RefreshToken = token;
            RefreshTokenExpiry = expiry;
        }

    }
}
