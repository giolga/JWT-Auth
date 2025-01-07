﻿using Microsoft.AspNetCore.Identity.UI.Services;

namespace JwtAuth.Services
{
    public class FakeEmailSender : IEmailSender
    {
        public Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            Console.WriteLine("Fake Email Sent!");
            Console.WriteLine($"To: {email}");
            Console.WriteLine($"Subject: {subject}");
            Console.WriteLine("Message:");
            Console.WriteLine(htmlMessage);
            return Task.CompletedTask;
        }
    }
}
