[![Contributors][contributors-shield]][contributors-url]
[![Forks][forks-shield]][forks-url]
[![Stargazers][stars-shield]][stars-url]
[![Issues][issues-shield]][issues-url]
[![project_license][license-shield]][license-url]

# Contacts-App-Backend

A lightweight, high-performance RESTful API built with **ASP.NET Core 8 Minimal APIs**. Designed to serve as the backend for the [Contacts App Clients](https://github.com/Alexzander-Hurd/Contacts-App-Clients), handling authentication, data persistence, and business logic.

[View on GitHub](https://github.com/Alexzander-Hurd/Contacts-App-Backend)

---

## 📜 Table of Contents

- [About The Project](#about-the-project)
- [Getting Started](#getting-started)
  - [Installation](#installation)
  - [Usage](#usage)
- [Roadmap](#roadmap)
- [Contributing](#contributing)
- [License](#license)
- [Contact](#contact)
- [Acknowledgements](#acknowledgements)
- [Security Policy](#security-policy)

---

## 🧠 About The Project

This project is the backbone of the Contacts App ecosystem, focusing on speed and simplicity by utilizing the Minimal API architecture instead of traditional MVC controllers.

Key capabilities include:

- **Minimal API Architecture:** Clean, low-overhead routing using `MapGroup`.
- **Secure Authentication:** Stateless JWT implementation with Role-Based Access Control (RBAC).
- **Data Integrity:** Code-first database management via Entity Framework Core.
- **OpenAPI Integration:** Auto-generated Swagger documentation for easy client consumption.

---

## 🛠 Built With

[![ASP.NET Core](https://img.shields.io/badge/ASP.NET_Core-6E4C7F?style=for-the-badge&logo=dotnet&logoColor=white)](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis)
[![EF Core](https://img.shields.io/badge/EF_Core-512BD4?style=for-the-badge&logo=.net&logoColor=white)](https://learn.microsoft.com/en-us/ef/core/)
[![SQLite](https://img.shields.io/badge/SQLite-2e94d6?style=for-the-badge&logo=sqlite&logoColor=white)](https://sqlite.org)

---

## 🚀 Getting Started

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

---

### Installation

Clone the repository:

```bash
git clone [https://github.com/Alexzander-Hurd/Contacts-App-Backend.git](https://github.com/Alexzander-Hurd/Contacts-App-Backend.git)
cd Contacts-App-Backend
```

Create the DB file for sqlite:

```bash
mkdir AppData
touch AppData/Contacts.db
```

Apply database migrations:

```bash
Auth__SecretKey="JWT signing secret" dotnet ef database update
```

Run the API:

```bash
Auth__SecretKey="JWT signing secret" dotnet run
```

---

## 💡 Usage

Once running, the API will be available at <http://localhost:5169>

1. Navigate to <http://localhost:5169/swagger> to view the interactive API documentation.
2. Use the /auth/register endpoint to create your initial Admin user.
3. Use the generated JWT to authenticate requests against the protected /contacts and /groups endpoints.

More detailed examples and screenshots will be added soon.

---

## 🛣 Roadmap

Planned key features include:

- Docker containerization for cloud deployment
- Rate limiting and advanced security middleware
- Webhook-based input
- Integration tests for critical auth flows

---

## 🤝 Contributing

Pull requests are welcome. For major changes, please open an issue first to discuss what you would like to change.

Contributions will be governed by a future [`CONTRIBUTING.md`](CONTRIBUTING.md).

---

## 🛡 License

Distributed under the MIT License. See [`LICENSE`](LICENSE) for more information.

© Alexzander Hurd

---

## 📬 Contact

- GitHub: [@Alexzander-Hurd](https://github.com/Alexzander-Hurd)
- Website: [alexhurd.uk](https://www.alexhurd.uk)
- Links: [alexhurd.uk/links](https://www.alexhurd.uk/links)

---

## 🛡 Security Policy

Security disclosures and vulnerability reports will be handled via a formal [`SECURITY.md`](SECURITY.md) in the near future. For now, please contact through GitHub issues or [alexhurd.uk](https://www.alexhurd.uk).

---

## 🙌 Acknowledgements

- [Best README Template](https://github.com/othneildrew/Best-README-Template)
- [.NET](https://dotnet.microsoft.com/)

[contributors-shield]: https://img.shields.io/github/contributors/Alexzander-Hurd/Contacts-App-Backend.svg?style=for-the-badge
[contributors-url]: https://github.com/Alexzander-Hurd/Contacts-App-Backend/graphs/contributors
[forks-shield]: https://img.shields.io/github/forks/Alexzander-Hurd/Contacts-App-Backend.svg?style=for-the-badge
[forks-url]: https://github.com/Alexzander-Hurd/Contacts-App-Backend/network/members
[stars-shield]: https://img.shields.io/github/stars/Alexzander-Hurd/Contacts-App-Backend.svg?style=for-the-badge
[stars-url]: https://github.com/Alexzander-Hurd/Contacts-App-Backend/stargazers
[issues-shield]: https://img.shields.io/github/issues/Alexzander-Hurd/Contacts-App-Backend.svg?style=for-the-badge
[issues-url]: https://github.com/Alexzander-Hurd/Contacts-App-Backend/issues
[license-shield]: https://img.shields.io/github/license/Alexzander-Hurd/Contacts-App-Backend.svg?style=for-the-badge
[license-url]: https://github.com/Alexzander-Hurd/Contacts-App-Backend/blob/master/LICENSE.txt