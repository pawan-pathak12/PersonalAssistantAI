# Personal Assistant AI

A simple AI personal assistant built with **C#** and **Semantic Kernel** that helps with tasks, reminders, document queries, and general assistance.  
It uses a plugin-based architecture so you can easily extend it with new capabilities.

## ✨ Features
- 💬 Natural language conversations
- 📝 Task management (add, view tasks)
- 💾 Conversation memory across sessions
- 🛡️ Input validation and error handling
- 🔌 Plugin-based architecture (Calculator, Weather, Time, PDF, etc.)
- 📄 **PDF integration**: load a PDF into the chat context with `/pdf <path>` and ask questions about its content
- 🌦️ **Real-time weather**: fetch current weather using the Weather plugin
- ⏰ **Real-time time**: get current system time or query time zones

## 🛠️ Technology Stack
- **C# .NET 9.0**
- **Semantic Kernel** + **Ollama** (local AI model execution)
- **File-based storage** (JSON for persistence)
- **Console interface**

## 🚀 Setup
1. Install [Ollama](https://ollama.ai) and pull a model (example: Qwen2.5 7B):
   ```bash
   ollama pull qwen2.5:7b