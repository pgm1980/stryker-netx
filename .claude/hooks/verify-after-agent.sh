#!/bin/bash
# SubagentStop Hook: Automatische Verifikation nach Agent-Rückkehr
# Führt Build, Test und Semgrep auf geänderte Dateien aus.
# Input: JSON mit agent_id, agent_type, agent_transcript_path via stdin
#
# Exit Codes:
#   0 = Alles OK, additionalContext mit Ergebnis
#   0 + Warnung im Output = Build/Test/Semgrep Probleme gefunden

set -uo pipefail

INPUT=$(cat)
RESULTS=""
HAS_ERRORS=false

# --- 1. Build prüfen ---
BUILD_OUTPUT=$(dotnet build --nologo --verbosity quiet 2>&1)
BUILD_EXIT=$?

if [ $BUILD_EXIT -eq 0 ]; then
  RESULTS="BUILD: OK (0 Warnings, 0 Errors)"
else
  RESULTS="BUILD: FAILED — Subagent hat Build-Fehler hinterlassen. Hauptsession MUSS fixen."
  HAS_ERRORS=true
  BUILD_ERRORS=$(echo "$BUILD_OUTPUT" | head -20)
  RESULTS="$RESULTS\n$BUILD_ERRORS"
fi

# --- 2. Tests prüfen (nur wenn Build OK) ---
if [ "$HAS_ERRORS" = false ]; then
  TEST_OUTPUT=$(dotnet test --nologo --verbosity quiet 2>&1)
  TEST_EXIT=$?

  if [ $TEST_EXIT -eq 0 ]; then
    RESULTS="$RESULTS\nTESTS: OK (alle grün)"
  else
    RESULTS="$RESULTS\nTESTS: FAILED — Subagent hat Test-Fehler hinterlassen."
    HAS_ERRORS=true
    TEST_ERRORS=$(echo "$TEST_OUTPUT" | grep -E "Failed|Error" | head -10)
    RESULTS="$RESULTS\n$TEST_ERRORS"
  fi
fi

# --- 3. Semgrep auf geänderte Dateien (nur .cs Dateien) ---
CHANGED_FILES=$(git diff --name-only HEAD 2>/dev/null | grep '\.cs$' || true)
if [ -n "$CHANGED_FILES" ]; then
  SEMGREP_OUTPUT=$(echo "$CHANGED_FILES" | xargs semgrep scan --config auto --quiet 2>&1)
  SEMGREP_EXIT=$?

  if [ $SEMGREP_EXIT -eq 0 ] && [ -z "$SEMGREP_OUTPUT" ]; then
    RESULTS="$RESULTS\nSEMGREP: OK (keine Findings)"
  else
    RESULTS="$RESULTS\nSEMGREP: FINDINGS — Security-Issues in geänderten Dateien."
    HAS_ERRORS=true
    SEMGREP_FINDINGS=$(echo "$SEMGREP_OUTPUT" | head -20)
    RESULTS="$RESULTS\n$SEMGREP_FINDINGS"
  fi
else
  RESULTS="$RESULTS\nSEMGREP: SKIP (keine geänderten .cs Dateien)"
fi

# --- Output ---
if [ "$HAS_ERRORS" = true ]; then
  echo "VERIFICATION FAILED after subagent return:"
  echo -e "$RESULTS"
  echo ""
  echo "ACTION REQUIRED: Hauptsession muss die Fehler beheben bevor weitergemacht wird."
else
  echo "VERIFICATION PASSED after subagent return:"
  echo -e "$RESULTS"
fi

exit 0
