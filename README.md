# ArtStudio

ArtStudio is an experimental desktop application exploring the combination of generative AI tools with basic user interface and graphics editing features.

## Project Goals

- Experiment with integrating generative AI into simple digital art and graphics editing workflows
- Provide a minimal, approachable UI for testing ideas and features
- Encourage learning, prototyping, and community feedback

## Features (Experimental / In Progress)

- Basic digital painting and drawing tools
- Simple layer management
- **Material Design UI** - Modern, responsive interface with custom theming
- **Professional Docking System** - AvalonDock integration for flexible workspaces
- Generative AI-powered image suggestions or enhancements
- Ability to use local and/or cloud-based AI models
- Basic image editing (crop, resize, simple filters)
- Minimal import/export support (PNG, JPEG)

## UI/UX Features

- **Custom Dark Theme** - Purple/indigo color scheme optimized for creative work
- **Material Design Components** - Modern buttons, cards, and controls
- **Dockable Panels** - Floating and dockable tool palettes, layers, and properties
- **Responsive Layout** - Adapts to different screen sizes and configurations
- **Icon-Rich Interface** - Material Design icons throughout the application

## Contributing

Contributions, ideas, and feedback are welcome! See [CONTRIBUTING.md](CONTRIBUTING.md) for guidelines.

### Getting Started

1. Clone the repository:
   ```bash
   git clone <repository-url>
   cd ArtStudio
   ```
2. Install dependencies:
   ```bash
   dotnet restore
   ```
3. Build the application:
   ```bash
   dotnet build --configuration Release
   ```
4. Run the application:
   ```bash
   dotnet run --project src/ArtStudio.WPF
   ```

### Development

#### Prerequisites

- .NET 8.0 SDK or later
- Windows 10/11 (for WPF support)
- Visual Studio 2022 or Visual Studio Code (recommended)

#### Running Tests

```bash
dotnet test
```

#### Publishing for Distribution

```bash
dotnet publish src/ArtStudio.WPF/ArtStudio.WPF.csproj --configuration Release --output ./publish --self-contained true --runtime win-x64 -p:PublishSingleFile=true
```

## License

The source code for ArtStudio is distributed under the MIT License. Any outputs, artworks, or generated content produced by the application are distributed under the GNU General Public License (GPL).

---

_This is a work in progress and an experimental project. Features and direction may change at any time._
