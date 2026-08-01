namespace ZenBlog.API.Endpoints.Registrations
{
    public static class EndpointRegistration
    {
        public static void RegisterEndpoints(this IEndpointRouteBuilder erb)
        {
            erb.RegisterCategoryEndpoints();
            erb.RegisterBlogEndpoints();
            erb.RegisterUserEndpoints();
            erb.RegisterCommentEndpoints();
            erb.RegisterAuthEndpoints();
            erb.RegisterSettingsEndpoints();
            erb.RegisterMediaEndpoints();
            erb.RegisterMonitoringEndpoints();
        }
    }
}
