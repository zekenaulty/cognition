# DoD Domains + Workflows v2 — Step 2026-01-01 00:39 — new DoD console scaffold

Goal
- Scaffold the new DoD-first React console project.

Context
- New console will replace the legacy console build output in `Cognition.Api`.

Commands Executed
- `npm create vite@latest Cognition.Console.Dod -- --template react-ts`
- `Remove-Item -Path src\Cognition.Console.Dod\src\main.ts, src\Cognition.Console.Dod\src\counter.ts, src\Cognition.Console.Dod\src\style.css, src\Cognition.Console.Dod\src\typescript.svg`
- `Set-Content -Path src\Cognition.Console.Dod\vite.config.ts`
- `Set-Content -Path src\Cognition.Console.Dod\src\main.tsx`
- `Set-Content -Path src\Cognition.Console.Dod\src\App.tsx`
- `Set-Content -Path src\Cognition.Console.Dod\src\styles.css`
- `Remove-Item -Path src\Cognition.Console.Dod\public\vite.svg`

Files Changed
- `src/Cognition.Console.Dod/index.html`
- `src/Cognition.Console.Dod/package.json`
- `src/Cognition.Console.Dod/tsconfig.json`
- `src/Cognition.Console.Dod/vite.config.ts`
- `src/Cognition.Console.Dod/src/main.tsx`
- `src/Cognition.Console.Dod/src/App.tsx`
- `src/Cognition.Console.Dod/src/styles.css`
- `src/Cognition.Console.Dod/src/main.ts` (removed)
- `src/Cognition.Console.Dod/src/counter.ts` (removed)
- `src/Cognition.Console.Dod/src/style.css` (removed)
- `src/Cognition.Console.Dod/src/typescript.svg` (removed)
- `src/Cognition.Console.Dod/public/vite.svg` (removed)

Tests / Results
- Not run (scaffold only).

Issues
- `npm create vite` defaulted to vanilla TS; converted to React manually.

Decision
- Use Vite + React + TypeScript with minimal dependencies and custom styling.

Completion
- [x]

Next Actions
- Update API build target to use the new console output and install npm deps.
