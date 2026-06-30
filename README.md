# DentalLink
DentalLink is a web-based dental platform that connects patients who need affordable dental treatment
with 5th-year dental students who require real clinical experience to complete their academic program. All
treatments are performed under the direct supervision of licensed dental faculty at registered universities.
The platform acts as a structured bridge: patients post dental cases, qualified students accept them, and
both parties track progress through a managed status workflow — with an administrator overseeing
student approvals, case monitoring, and platform integrity.
## Project Structure

```
DentalLink/
├── Areas/Admin/          # Admin area (controllers + views)
├── Controllers/          # Account, Home, Patient, Student
├── Data/
│   ├── ApplicationDbContext.cs
│   └── SeedData.cs
├── Models/               # Domain entities (DentalCase, Review, Notification, Testimonial …)
├── Services/
│   ├── Interfaces/       # IEmailService, IFileService
│   └── Implementations/  # EmailService, FileService
├── ViewModels/           # Account, Case, Testimonial view models
├── Views/                # Razor views per controller + Shared layout
├── wwwroot/
│   ├── css/dentbridge.css
│   ├── js/dentbridge.js
│   └── uploads/          # Runtime file storage
└── Migrations/
```

---

## Security

- Passwords: 8+ characters, uppercase, digit, and special character required
- Account lockout: 5 failed attempts → 15-minute lockout
- CSRF protection on all POST actions (`ValidateAntiForgeryToken`)
- Role-based `[Authorize]` attributes on every protected controller
- Session cookie: 7-day sliding expiration
- File uploads validated by extension and size (images ≤ 5 MB, documents ≤ 10 MB)

---

## License

This project was developed as a graduation project.
