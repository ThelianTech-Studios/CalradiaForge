# CalradiaForge.ConsoleUtils

## Project Overview
CalradiaForge.ConsoleUtils is a developer-only console application that reuses Core services.

## Purpose
Provide lightweight maintenance or generation utilities without running the WPF app.

## Responsibilities
### Currently Implemented
- Present a simple console menu.
- Generate the default English language file from Core translation defaults.

## Public Boundaries
- This project depends on Core only.
- It should remain tooling-focused rather than becoming a second runtime app.

## Dependencies
### Currently Implemented
- `CalradiaForge.Core`

## Major Systems
- Language file generation utility

## Design Constraints
- Keep the project for developer support tasks.
- Do not move UI runtime behavior into this project.

## Known Extension Points
- Future maintenance utilities can reuse Core services if they stay clearly developer-focused.

## Deferred Work
- No additional tooling is currently implemented in source.
