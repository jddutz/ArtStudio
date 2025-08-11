# GitHub Copilot Agent Work Area

This directory serves as the designated work area for GitHub Copilot to manage temporary files, work plans, checklists, task instructions, scripts, and project management documents.

## Directory Structure

### `tasks/`

- Active task definitions and instructions
- Task progress tracking files
- Task completion checklists

### `plans/`

- Project plans and roadmaps
- Feature implementation strategies
- Architecture planning documents

### `scripts/`

- Utility scripts for automation
- Build and deployment helpers
- Data processing scripts

## Usage Guidelines

1. **Temporary Files**: Use this area for any working documents that don't belong in the main project structure
2. **Organization**: Create subdirectories as needed for better organization
3. **Naming**: Use descriptive filenames, include dates when appropriate
4. **Cleanup**: Remove temporary files after task completion when appropriate
5. **Documentation**: Keep important decisions and plans documented here for future reference

## File Naming Conventions

- Task files: `task-YYYY-MM-DD-description.md`
- Plans: `plan-feature-name.md` or `roadmap-YYYY-MM.md`
- Scripts: `script-purpose.ps1` or `script-purpose.sh`
- Checklists: `checklist-task-description.md`

This directory is referenced in the main `.copilot.instructions` file and should be used consistently by GitHub Copilot for all working documents.
