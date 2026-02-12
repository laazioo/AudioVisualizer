# AudioVisualizer

A WPF application for visualizing system audio.

## Prerequisites

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) or later.
- Windows OS (WPF is Windows-only).

## How to Run

1. Open a terminal in the project directory:
   ```powershell
   cd .\AudioVisualizer
   ```

2. Run the application using the `dotnet` CLI:
   ```powershell
   dotnet run
   ```

3. Alternatively, you can build and run the executable:
   ```powershell
   dotnet build
   .\bin\Debug\net8.0-windows\AudioVisualizer.exe
   ```

## Troubleshooting

- If you encounter errors about missing dependencies, run:
  ```powershell
  dotnet restore
  ```

- Ensure your microphone or system audio loopback device is enabled and accessible.
