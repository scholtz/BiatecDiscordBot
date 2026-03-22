# Biatec Discord Bot

A robust Discord bot built with .NET 10, Entity Framework Core, and Discord.Net that prompts users with questions, collects responses via multiple choice options, and assigns roles based on answers.

## Features

- **User Prompting**: Send multiple-choice questions to Discord users via DM
- **Role Assignment**: Automatically assign Discord roles based on user answers
- **Message Tracking**: Track all incoming and outgoing Discord messages
- **REST API with Swagger**: Full API for managing questions, users, and messages
- **Entity Framework Core**: PostgreSQL for production, InMemory for local development
- **Kubernetes Ready**: Complete deployment manifests for K8s with PostgreSQL
- **CI/CD Pipeline**: GitHub Actions for building, testing, and deploying to Harbor registry

## Architecture

```
BiatecDiscordBot/
├── Controllers/          # REST API controllers
│   ├── DiscordController.cs    # Guild users, messages, roles
│   ├── QuestionsController.cs  # Question CRUD and answers
│   └── MessagesController.cs   # Message tracking queries
├── Data/
│   └── BotDbContext.cs         # EF Core database context
├── Models/
│   ├── DiscordUserEntity.cs    # Discord user entity
│   ├── Question.cs             # Question entity
│   ├── QuestionOption.cs       # Multiple choice option
│   ├── UserAnswer.cs           # User answer record
│   ├── TrackedMessage.cs       # Message tracking entity
│   └── DTOs/                   # Data transfer objects
├── Services/
│   ├── IDiscordBotService.cs   # Discord bot interface
│   ├── DiscordBotService.cs    # Discord.Net implementation
│   ├── IMessageTrackingService.cs
│   ├── MessageTrackingService.cs
│   ├── IQuestionService.cs
│   ├── QuestionService.cs
│   └── DiscordBotHostedService.cs  # Background service
├── k8s/                    # Kubernetes manifests
├── .github/workflows/      # CI/CD pipelines
└── BiatecDiscordBot.Tests/  # xUnit test project
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Discord Bot Token (from [Discord Developer Portal](https://discord.com/developers/applications))
- PostgreSQL (for production) or use InMemory database for development

### Local Development

1. **Clone the repository**
   ```bash
   git clone https://github.com/scholtz/BiatecDiscordBot.git
   cd BiatecDiscordBot
   ```

2. **Configure the Discord bot token** using user secrets:
   ```bash
   cd BiatecDiscordBot
   dotnet user-secrets set "Discord:Token" "your-bot-token-here"
   ```

3. **Run the application** (uses InMemory database by default):
   ```bash
   dotnet run --project BiatecDiscordBot
   ```

4. **Access Swagger UI** at `http://localhost:5201`

### Configuration

The application uses the following configuration (via `appsettings.json` or environment variables):

| Setting | Description | Default |
|---------|-------------|---------|
| `Discord:Token` | Discord bot token | (empty - bot won't start) |
| `UseInMemoryDatabase` | Use EF Core InMemory provider | `true` |
| `ConnectionStrings:DefaultConnection` | PostgreSQL connection string | (empty) |

### Running Tests

```bash
dotnet test --verbosity normal
```

## API Endpoints

### Discord Operations (`/api/discord`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/discord/status` | Get bot connection status |
| GET | `/api/discord/guilds/{guildId}/users` | Get all users from a guild |
| POST | `/api/discord/messages/send` | Send a DM to a user |
| POST | `/api/discord/channels/{channelId}/messages` | Send a channel message |
| POST | `/api/discord/guilds/{guildId}/users/{userId}/roles/{roleId}` | Assign a role |
| POST | `/api/discord/users/{userId}/questions/{questionId}` | Send a question to a user |

### Questions (`/api/questions`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/questions` | Create a new question with options |
| GET | `/api/questions/{questionId}` | Get a specific question |
| GET | `/api/questions/guild/{guildId}` | Get active questions for a guild |
| POST | `/api/questions/answer` | Record a user's answer |
| GET | `/api/questions/answers/{discordUserId}` | Get user's answers |
| DELETE | `/api/questions/{questionId}` | Deactivate a question |

### Messages (`/api/messages`)

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/messages/guild/{guildId}` | Get messages by guild (paginated) |
| GET | `/api/messages/user/{userId}` | Get messages by user (paginated) |
| GET | `/api/messages/direction/{direction}` | Get messages by direction |

## Deployment

### Docker

```bash
docker build -t biatec-discord-bot -f BiatecDiscordBot/Dockerfile .
docker run -e Discord__Token=your-token -e UseInMemoryDatabase=false \
  -e ConnectionStrings__DefaultConnection="Host=db;Database=biatecdiscordbot;Username=botuser;Password=secret" \
  -p 8080:8080 biatec-discord-bot
```

### Kubernetes

1. **Create namespace and secrets**:
   ```bash
   kubectl apply -f k8s/namespace.yaml
   # Edit k8s/secrets.yaml and k8s/postgresql.yaml with your actual secrets
   kubectl apply -f k8s/postgresql.yaml
   kubectl apply -f k8s/secrets.yaml
   ```

2. **Deploy the application**:
   ```bash
   # Update the image reference in k8s/deployment.yaml
   kubectl apply -f k8s/deployment.yaml
   ```

### CI/CD (GitHub Actions)

The pipeline automatically:
1. Builds and tests on every push/PR
2. Builds Docker image and pushes to Harbor registry on main branch
3. Deploys to Kubernetes on main branch

**Required GitHub Secrets**:
- `HARBOR_USERNAME` - Harbor registry username
- `HARBOR_PASSWORD` - Harbor registry password
- `KUBE_CONFIG` - Base64-encoded kubeconfig

## Discord Bot Setup

1. Go to [Discord Developer Portal](https://discord.com/developers/applications)
2. Create a new application
3. Go to the **Bot** section and create a bot
4. Enable the following **Privileged Gateway Intents**:
   - Server Members Intent
   - Message Content Intent
5. Copy the bot token and configure it in the application
6. Invite the bot to your server using the OAuth2 URL Generator with scopes: `bot`, `applications.commands`
7. Required bot permissions: `Manage Roles`, `Send Messages`, `Read Message History`

## License

This project is licensed under the MIT License.
