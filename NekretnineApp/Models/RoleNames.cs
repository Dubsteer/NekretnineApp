namespace NekretnineApp.Models
{
    public static class RoleNames
    {
        public const string Admin = "Admin";
        public const string Advertiser = "Advertiser";
        public const string User = "User";

        public static readonly string[] PublicRoles = { Advertiser, User };

        public static string DisplayName(string role)
        {
            return role switch
            {
                Admin => "Administrator",
                Advertiser => "Oglašivač",
                User => "Korisnik",
                _ => role
            };
        }
    }
}
