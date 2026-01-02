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
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Name is required", nameof(name));
            }

            // email validation
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new ArgumentException("Email is required", nameof(email));
            }

            int index = email.IndexOf('@');
            if (!(index > 0 && index != email.Length - 1 && index == email.LastIndexOf('@')))
            {
                throw new ArgumentException("Invalid email", nameof(email));
            }

            Id = Guid.NewGuid();
            Name = name.Trim();
            Email = email.Trim();
            CreatedAtUtc = DateTime.UtcNow;
        }

    }
}
