#!/usr/bin/env bash
#
# Setup the developer environment on Linux (e.g. Ubuntu).
# **This script is not run on build machines**
#
# Use this script to idempotently install the tools needed to build dirs.proj on Linux.
#
# Notes:
#   - net472 (the .NET Framework) cannot be built or run on Linux, so a Linux build only
#     produces the netstandard2.0/net8.0/net10.0 targets. This is handled automatically
#     by the MSBuild props (see $(IsWindowsBuild) in Directory.Build.props) - you don't
#     need to pass any extra flags.
#   - Build with:   dotnet build "$BASEDIR/dirs.proj"
#   - Run unit tests for a project with: dotnet test <path-to-test.csproj>
#
set -euo pipefail

BASEDIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DOTNET_SDK_VERSION="$(grep -o '"version"[[:space:]]*:[[:space:]]*"[^"]*"' "$BASEDIR/global.json" | head -1 | sed -E 's/.*"([^"]+)"$/\1/')"
if [ -z "$DOTNET_SDK_VERSION" ]; then
  echo "Error: could not read sdk.version from $BASEDIR/global.json" >&2
  exit 1
fi
DOTNET_INSTALL_DIR="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"

echo "Enlistment root: $BASEDIR"

# ---------------------------------------------------------------------------
# perl - required to generate DDL event code (SmoBuild/DdlEvents/*.pl)
# ---------------------------------------------------------------------------
if ! command -v perl >/dev/null 2>&1; then
  echo "perl not found, installing..."
  if command -v apt-get >/dev/null 2>&1; then
    sudo apt-get update
    sudo apt-get install -y perl
  else
    echo "Warning: apt-get not found. Please install perl manually and re-run this script." >&2
  fi
else
  echo "perl already installed: $(command -v perl)"
fi

# ---------------------------------------------------------------------------
# .NET SDK - required to build and test the repo
# ---------------------------------------------------------------------------
has_required_sdk() {
  command -v dotnet >/dev/null 2>&1 && dotnet --list-sdks 2>/dev/null | grep -q "^${DOTNET_SDK_VERSION} "
}

if has_required_sdk; then
  echo ".NET SDK $DOTNET_SDK_VERSION already installed: $(command -v dotnet)"
else
  echo ".NET SDK $DOTNET_SDK_VERSION not found, installing to $DOTNET_INSTALL_DIR..."
  curl -sSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  bash /tmp/dotnet-install.sh --version "$DOTNET_SDK_VERSION" --install-dir "$DOTNET_INSTALL_DIR"
  rm -f /tmp/dotnet-install.sh

  export PATH="$DOTNET_INSTALL_DIR:$PATH"
  if ! grep -q "$DOTNET_INSTALL_DIR" "$HOME/.bashrc" 2>/dev/null; then
    {
      echo ""
      echo "# Added by init.sh for sqlmanagementobjects"
      echo "export DOTNET_ROOT=\"$DOTNET_INSTALL_DIR\""
      echo "export PATH=\"$DOTNET_INSTALL_DIR:\$PATH\""
    } >> "$HOME/.bashrc"
    echo "Added $DOTNET_INSTALL_DIR to PATH in ~/.bashrc (start a new shell, or run 'source ~/.bashrc')."
  fi
fi

# ---------------------------------------------------------------------------
# dotnet global tools used by the repo
# ---------------------------------------------------------------------------
install_global_tool_if_missing() {
  local tool_name="$1"
  if dotnet tool list --global 2>/dev/null | grep -qi "^${tool_name} "; then
    echo "$tool_name already installed as a global dotnet tool"
  else
    dotnet tool install --global "$tool_name"
  fi
}

install_global_tool_if_missing "Microsoft.VisualStudio.SlnGen.Tool"
install_global_tool_if_missing "Microsoft.SqlPackage"

echo
echo
echo "To build for SMO development and to run unit tests:"
echo "   dotnet build $BASEDIR/dirs.proj"
echo
echo "Note: net472 (the .NET Framework) is skipped on Linux; the build produces"
echo "      the netstandard2.0/net8.0/net10.0 targets only."
echo
echo "To run tests for a single test project:"
echo "   dotnet test $BASEDIR/src/UnitTest/Smo/Microsoft.SqlServer.Test.SmoUnitTests.csproj"
echo
echo "To run functional tests, see $BASEDIR/src/FunctionalTest/runtests.cmd for the list"
echo "of filters and environment variables (e.g. SqlTestTargetServersFilter) that are"
echo "used to select which tests/backends to run, then invoke with dotnet test."
