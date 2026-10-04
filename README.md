# BTL-LTM-Plus — Multiplayer Word Puzzle

A real-time multiplayer word puzzle game built with **Unity**, made for the Network Programming course (PTIT).

## Features

- **Multiplayer rooms**: create or join a room for a live match
- **Real-time sync**: Kahoot-style flow, every player sees the same board and round timer
- **Core mechanic**: race to find hidden words on a letter grid, scores update instantly
- **Backend**: custom server over raw TCP sockets

## Gameplay

Each level is a square letter board (4x4, 5x5, ...) hiding a list of words grouped by category.
Drag across adjacent tiles to spell a word; found words lock in and the remaining letters fall into place.
See [`Assets/WordGame/GAMEPLAY.md`](Assets/WordGame/GAMEPLAY.md) and
[`Assets/CLIENT_GAMEPLAY_DETAILS.md`](Assets/CLIENT_GAMEPLAY_DETAILS.md) for the full client design
(`GameManager`, `LetterBoard`, `WordGrid`, board state save/load).

## Getting started

1. Open the project folder in Unity Hub.
2. Open the main scene and press Play.
3. Start the TCP server, then connect two or more clients to play together.
