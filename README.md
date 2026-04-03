# Quisly - Live Quiz Game

A real-time multiplayer quiz application built with Blazor WebAssembly and Supabase, featuring a vibrant Kahoot-style game show aesthetic.

## Tech Stack

- **Frontend:** Blazor WebAssembly (.NET 9)
- **Backend/Realtime:** Supabase (Auth, PostgreSQL, Realtime)
- **Styling:** Custom CSS with game-show aesthetic (no framework dependency)
- **QR Codes:** QRCoder

## Setup

### 1. Create a Supabase Project

1. Go to [supabase.com](https://supabase.com) and create a new project.
2. Copy your **Project URL** and **anon public key** from Settings > API.

### 2. Run the Database Schema

1. Open the SQL Editor in your Supabase dashboard.
2. Paste the contents of `supabase-schema.sql` and run it.
3. This creates all 5 tables (profiles, quizzes, questions, sessions, players) with Row Level Security and Realtime enabled.

### 3. Configure the App

Edit `wwwroot/appsettings.json` with your Supabase credentials:

```json
{
  "Supabase": {
    "Url": "https://YOUR_PROJECT_ID.supabase.co",
    "AnonKey": "YOUR_SUPABASE_ANON_KEY"
  }
}
```

### 4. Run the App

```bash
dotnet run
```

The app will be available at `https://localhost:5001` (or the port shown in the console).

## Project Structure

```
Quisly/
├── Models/              # Data models matching Supabase tables
│   ├── Profile.cs       # User profiles (Normal/Premium)
│   ├── Quiz.cs          # Quiz definitions
│   ├── Question.cs      # Questions with 4 options
│   ├── GameSession.cs   # Live game sessions
│   └── Player.cs        # Players in a session
├── Services/            # Business logic and Supabase integration
│   ├── SupabaseService.cs          # Supabase client wrapper
│   ├── AuthService.cs              # Authentication (sign up/in/out)
│   ├── GameService.cs              # Game CRUD and scoring
│   ├── RealtimeService.cs          # Realtime subscriptions
│   └── SupabaseAuthStateProvider.cs # Blazor auth integration
├── Components/          # Reusable UI components
│   ├── GameButton.razor       # Colored answer buttons (Red/Blue/Yellow/Green)
│   ├── CountdownTimer.razor   # Animated countdown ring
│   ├── QRCodeDisplay.razor    # QR code for joining games
│   ├── PinDisplay.razor       # Game PIN display
│   ├── Leaderboard.razor      # Top 5 scoreboard
│   └── Podium.razor           # Final podium (1st/2nd/3rd)
├── Pages/               # Route pages
│   ├── Auth/
│   │   ├── Login.razor
│   │   └── Register.razor
│   ├── Game/
│   │   ├── HostSetup.razor    # Choose quiz to host
│   │   ├── CreateQuiz.razor   # Build questions
│   │   ├── Lobby.razor        # Waiting room with PIN/QR
│   │   ├── HostGame.razor     # Host's game view
│   │   └── PlayGame.razor     # Player's game view
│   ├── Home.razor
│   └── Join.razor
├── Layout/
│   └── MainLayout.razor
├── wwwroot/
│   ├── css/app.css      # Full theme (game-show aesthetic)
│   ├── appsettings.json # Supabase config
│   └── index.html
├── supabase-schema.sql  # Database schema (run in Supabase SQL Editor)
├── App.razor
├── Program.cs
└── Quisly.csproj
```

## Database Schema

| Table      | Description                                    |
|------------|------------------------------------------------|
| profiles   | User profiles with `account_type` (Normal/Premium) |
| quizzes    | Quiz definitions created by hosts              |
| questions  | Questions with 4 options and correct answer    |
| sessions   | Live game sessions with 6-digit PIN            |
| players    | Players in a session with scores and streaks   |

## Game Flow

1. **Host** creates a quiz with questions
2. **Host** starts a game session (generates a 6-digit PIN + QR code)
3. **Players** join via PIN or QR code, appear in lobby
4. **Host** starts the game — questions sync in real-time
5. **Players** answer within the countdown timer
6. **Scoring** based on speed and accuracy, with streak bonuses
7. **Leaderboard** shown after each question
8. **Final podium** displayed at game end

## Account Types

- **Normal**: Default tier. Can host and play games.
- **Premium**: Enhanced tier stored in profile metadata. Can be extended with premium features.
