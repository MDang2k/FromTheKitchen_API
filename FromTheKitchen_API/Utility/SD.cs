namespace FromTheKitchen_API.Utility
{
    public static class SD
    {
        public const string Role_Admin = "Admin";
        public const string Role_Customer = "Customer";

        public const string status_confirmed = "Confirmed";
        public const string status_readyForPickUp = "Ready For Pick Up";
        public const string status_completed = "Completed";
        public const string status_cancelled = "Cancelled";

        /// <summary>
        /// Where uploaded menu images are saved and served from (as /images/...).
        /// ImageStorage:Path in hosting (/home/data/images on App Service survives redeploys), wwwroot/images otherwise.
        /// </summary>
        public static string ImagesFolder(IConfiguration config, IWebHostEnvironment env) =>
            config["ImageStorage:Path"] is { Length: > 0 } path ? path : Path.Combine(env.WebRootPath, "images");
    }
}
