namespace ECommerce.UserService.Domain
{
    public sealed class User
    {
        public Guid Id { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public DateTime CreatedAtUtc {  get; set; }

        public User(string name, string email)
        {
            // name validation
            if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Name is required", nameof(name));
            var n = name.Trim();
            if (n.Length > 200) throw new ArgumentException("Name too long (max 200).", nameof(name));

            // email validation
            if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("Email is required", nameof(email));
            var e = email.Trim().ToLowerInvariant();
            if (e.Length > 200) throw new ArgumentException("Email too long (max 200).", nameof(email));

            int index = email.IndexOf('@');
            if (!(index > 0 && index != email.Length - 1 && index == email.LastIndexOf('@')))
            {
                throw new ArgumentException("Invalid email", nameof(email));
            }

            Id = Guid.NewGuid();
            Name = n;
            Email = e;
            CreatedAtUtc = DateTime.UtcNow;
        }

    }
}
