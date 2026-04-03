-- ============================================
-- QUISLY - Supabase Database Schema
-- Run this SQL in the Supabase SQL Editor
-- ============================================

-- Enable UUID generation
create extension if not exists "uuid-ossp";

-- ============================================
-- 1. PROFILES
-- Linked to Supabase Auth users
-- ============================================
create table public.profiles (
    id          uuid primary key references auth.users(id) on delete cascade,
    username    text unique not null,
    display_name text not null,
    avatar_url  text,
    account_type text not null default 'Normal' check (account_type in ('Normal', 'Premium')),
    total_games_played integer not null default 0,
    total_games_hosted integer not null default 0,
    total_games_won    integer not null default 0,
    xp                 integer not null default 0,
    level              integer not null default 1,
    created_at  timestamptz not null default now()
);

comment on table public.profiles is 'User profiles extending Supabase Auth';
comment on column public.profiles.account_type is 'Normal or Premium tier';

-- Auto-create a profile when a new user signs up
create or replace function public.handle_new_user()
returns trigger as $$
begin
    insert into public.profiles (id, username, display_name)
    values (
        new.id,
        coalesce(new.raw_user_meta_data->>'username', 'user_' || left(new.id::text, 8)),
        coalesce(new.raw_user_meta_data->>'username', 'Player')
    );
    return new;
end;
$$ language plpgsql security definer;

create trigger on_auth_user_created
    after insert on auth.users
    for each row execute function public.handle_new_user();

-- ============================================
-- 2. QUIZZES
-- Quiz definitions created by users
-- ============================================
create table public.quizzes (
    id          uuid primary key default uuid_generate_v4(),
    creator_id  uuid not null references public.profiles(id) on delete cascade,
    title       text not null,
    description text,
    is_public   boolean not null default false,
    question_count integer not null default 0,
    created_at  timestamptz not null default now(),
    updated_at  timestamptz not null default now()
);

comment on table public.quizzes is 'Quiz templates created by hosts';

create index idx_quizzes_creator on public.quizzes(creator_id);

-- ============================================
-- 3. QUESTIONS
-- Individual questions belonging to a quiz
-- ============================================
create table public.questions (
    id              uuid primary key default uuid_generate_v4(),
    quiz_id         uuid not null references public.quizzes(id) on delete cascade,
    question_text   text not null,
    image_url       text,
    option_a        text not null,
    option_b        text not null,
    option_c        text not null,
    option_d        text not null,
    correct_option  text not null default 'A' check (correct_option in ('A', 'B', 'C', 'D')),
    time_limit_seconds integer not null default 20 check (time_limit_seconds between 5 and 120),
    points          integer not null default 1000,
    order_index     integer not null default 0
);

comment on table public.questions is 'Quiz questions with 4 options (A/B/C/D)';

create index idx_questions_quiz on public.questions(quiz_id);

-- ============================================
-- 4. SESSIONS (Live Game Sessions)
-- A running instance of a quiz
-- ============================================
create table public.sessions (
    id                      uuid primary key default uuid_generate_v4(),
    host_id                 uuid not null references public.profiles(id) on delete cascade,
    quiz_id                 uuid not null references public.quizzes(id) on delete cascade,
    pin                     text unique not null,
    status                  text not null default 'lobby'
                            check (status in ('lobby', 'playing', 'showing_question', 'showing_results', 'leaderboard', 'finished')),
    current_question_index  integer not null default 0,
    started_at              timestamptz,
    ended_at                timestamptz,
    created_at              timestamptz not null default now()
);

comment on table public.sessions is 'Live game sessions identified by 6-digit PIN';

create index idx_sessions_pin on public.sessions(pin);
create index idx_sessions_host on public.sessions(host_id);

-- ============================================
-- 5. PLAYERS
-- Players who joined a session
-- ============================================
create table public.players (
    id                  uuid primary key default uuid_generate_v4(),
    session_id          uuid not null references public.sessions(id) on delete cascade,
    user_id             uuid references public.profiles(id) on delete set null,
    nickname            text not null,
    score               integer not null default 0,
    current_streak      integer not null default 0,
    last_answer         text check (last_answer in ('A', 'B', 'C', 'D') or last_answer is null),
    last_answer_time_ms integer,
    last_answer_correct boolean,
    joined_at           timestamptz not null default now()
);

comment on table public.players is 'Players in a game session with scores';

create index idx_players_session on public.players(session_id);
create index idx_players_user on public.players(user_id);

-- ============================================
-- ROW LEVEL SECURITY (RLS)
-- ============================================

alter table public.profiles enable row level security;
alter table public.quizzes enable row level security;
alter table public.questions enable row level security;
alter table public.sessions enable row level security;
alter table public.players enable row level security;

-- Profiles: users can read all profiles, update only their own
create policy "Profiles are viewable by everyone"
    on public.profiles for select using (true);

create policy "Users can update own profile"
    on public.profiles for update using (auth.uid() = id);

create policy "Users can insert own profile"
    on public.profiles for insert with check (auth.uid() = id);

-- Quizzes: public quizzes readable by all, own quizzes fully managed
create policy "Public quizzes are viewable by everyone"
    on public.quizzes for select using (is_public or auth.uid() = creator_id);

create policy "Users can create quizzes"
    on public.quizzes for insert with check (auth.uid() = creator_id);

create policy "Users can update own quizzes"
    on public.quizzes for update using (auth.uid() = creator_id);

create policy "Users can delete own quizzes"
    on public.quizzes for delete using (auth.uid() = creator_id);

-- Questions: readable if quiz is accessible
create policy "Questions viewable with quiz access"
    on public.questions for select using (
        exists (
            select 1 from public.quizzes q
            where q.id = quiz_id
            and (q.is_public or q.creator_id = auth.uid())
        )
    );

create policy "Quiz creators can manage questions"
    on public.questions for insert with check (
        exists (
            select 1 from public.quizzes q
            where q.id = quiz_id and q.creator_id = auth.uid()
        )
    );

create policy "Quiz creators can update questions"
    on public.questions for update using (
        exists (
            select 1 from public.quizzes q
            where q.id = quiz_id and q.creator_id = auth.uid()
        )
    );

create policy "Quiz creators can delete questions"
    on public.questions for delete using (
        exists (
            select 1 from public.quizzes q
            where q.id = quiz_id and q.creator_id = auth.uid()
        )
    );

-- Sessions: anyone can view active sessions (to join by PIN), hosts manage
create policy "Active sessions are viewable"
    on public.sessions for select using (true);

create policy "Authenticated users can create sessions"
    on public.sessions for insert with check (auth.uid() = host_id);

create policy "Hosts can update own sessions"
    on public.sessions for update using (auth.uid() = host_id);

-- Players: viewable within a session, anyone can join
create policy "Players are viewable in session"
    on public.players for select using (true);

create policy "Anyone can join a session"
    on public.players for insert with check (true);

create policy "Players can update own record"
    on public.players for update using (auth.uid() = user_id or user_id is null);

-- ============================================
-- REALTIME
-- Enable realtime for sessions and players
-- ============================================
alter publication supabase_realtime add table public.sessions;
alter publication supabase_realtime add table public.players;

-- ============================================
-- MIGRATION: XP, Level & Wins columns
-- Run this if you have an existing profiles table
-- ============================================
alter table public.profiles
    add column if not exists xp              integer not null default 0,
    add column if not exists level           integer not null default 1,
    add column if not exists total_games_won integer not null default 0;

-- ============================================
-- HELPER FUNCTION: Update question count
-- ============================================
create or replace function public.update_question_count()
returns trigger as $$
begin
    if TG_OP = 'INSERT' or TG_OP = 'UPDATE' then
        update public.quizzes
        set question_count = (select count(*) from public.questions where quiz_id = NEW.quiz_id),
            updated_at = now()
        where id = NEW.quiz_id;
    end if;
    if TG_OP = 'DELETE' then
        update public.quizzes
        set question_count = (select count(*) from public.questions where quiz_id = OLD.quiz_id),
            updated_at = now()
        where id = OLD.quiz_id;
    end if;
    return coalesce(NEW, OLD);
end;
$$ language plpgsql security definer;

create trigger on_question_change
    after insert or update or delete on public.questions
    for each row execute function public.update_question_count();
