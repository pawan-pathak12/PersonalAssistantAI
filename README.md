Personal Assistant AI - JARVIS Edition
A sophisticated AI personal assistant built with C# and Semantic Kernel that functions as your personal JARVIS with full voice capabilities. Features intelligent conversation, real-time data access, and bidirectional voice interaction.

# ✨ Features
    ## 🎙️ Voice Input - Speak naturally using Whisper speech-to-text
    ## 🔊 Voice Output - JARVIS-style responses via Text-to-Speech

    💬 Natural language conversations with JARVIS personality

    📝 Task management (add, view tasks)

    💾 Conversation memory across sessions

    🛡️ Input validation and error handling

    🔌 Plugin-based architecture (Calculator, Weather, Time, PDF, Web Search)

    📄 PDF integration: Load PDFs with /pdf <path> and ask questions about content

    🌦️ Real-time weather: Fetch current weather using Weather plugin

    ⏰ Real-time time: Get current system time

    🔍 Web search: Access current information via /search <query>

    🎯 JARVIS Personality - Professional, intelligent, and proactive assistant

# 🛠️ Technology Stack
    C# .NET 9.0

    Semantic Kernel + Ollama (local AI model execution)

    Whisper (OpenAI's speech recognition for voice input)

    System.Speech.Synthesis (Text-to-speech for voice output)

    File-based storage (JSON for conversation persistence)

    FFmpeg (Audio recording and processing)

    Console interface with voice control

# 🎙️ Voice System
    Speech-to-Text: Whisper with ggml-tiny.en.bin model

    Text-to-Speech: Windows System.Speech with configurable voices

    Voice Control:

    Automatic 10-second listening sessions

    Background noise filtering

    Non-interfering audio (stops listening while speaking)

    Voice toggle with voice command

# 🚀 Setup
#  1. Install Ollama & Model
    bash
    ollama pull qwen2.5:7b
 #2. Install Whisper

    Download whisper-cli from OpenAI Whisper repository
    Place in C:\whisper\Release\
    Download model ggml-tiny.en.bin to C:\whisper\models\

# 3. Install FFmpeg
    Download FFmpeg and ensure it's in system PATH

    Required for audio recording from microphone

# 4. Configure API Keys
    Add to appsettings.json:

    json
    {
      "GoogleSearch": {
        "ApiKey": "your_google_api_key",
        "SearchEngineId": "your_search_engine_id"
      }
    }
    🎯 Usage
    Speak naturally - System automatically listens for 10 seconds

    Type commands - Use text input when preferred

    Voice toggle - Type voice to enable/disable speech responses

    PDF queries - /pdf path/to/file.pdf

    Web search - /search your query

    Exit - Type q or quit

# 🔧 Plugins
    WeatherPlugin - Real-time weather data

    TimePlugin - Current time and timezone information

    CalculatorPlugin - Mathematical calculations

    PdfPlugin - Document analysis and Q&A

    WebSearchService - Internet information retrieval

# 💡 JARVIS Personality
    Your assistant embodies JARVIS characteristics:

    Intelligent and analytical responses

    Professional yet approachable tone

    Proactive assistance

    Clear, concise explanations

    Sophisticated but natural communication

    Built with a focus on practical AI engineering and agentic systems development.