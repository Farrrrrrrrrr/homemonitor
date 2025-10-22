#!/usr/bin/env bash
# Lightweight helper to run the backend from the repo root with sensible defaults.
# Usage: ./run-backend.sh [port]
# If port is omitted it defaults to 5000.
#!/usr/bin/env bash
# Lightweight helper to run the backend from the repo root with sensible defaults.
# Usage: ./run-backend.sh [port]
# If port is omitted it defaults to 5000.

set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_PROJECT="$ROOT_DIR/backend/Backend.csproj"

PORT="${1:-5000}"

export ASPNETCORE_URLS="http://0.0.0.0:${PORT}"

echo "Starting backend using project: $BACKEND_PROJECT"
echo "Listening on $ASPNETCORE_URLS"

# Quick runtime check: ensure Microsoft.AspNetCore.App is available
if ! dotnet --list-runtimes 2>/dev/null | grep -q "Microsoft.AspNetCore.App"; then
	cat <<'MSG'
ERROR: Microsoft.AspNetCore.App runtime was not found.

This application requires the ASP.NET Core runtime (Microsoft.AspNetCore.App).
On Arch Linux you can try installing the runtime package if it's available in your repos:

	sudo pacman -Ss aspnet
	# if found, install (package name may be: dotnet-aspnet-runtime)
	sudo pacman -S dotnet-aspnet-runtime

If the package is not available in your distribution repositories, you can install the runtime locally
using the official dotnet-install script (installs into $HOME/.dotnet):

	curl -sSL https://dot.net/v1/dotnet-install.sh -o ./dotnet-install.sh
	chmod +x ./dotnet-install.sh
	./dotnet-install.sh --runtime aspnetcore --version 9.0.9 --install-dir $HOME/.dotnet

Then add the install dir to your PATH for the current session and retry:

	export PATH="$HOME/.dotnet:$PATH"
	export DOTNET_ROOT="$HOME/.dotnet"

After installing the ASP.NET runtime, re-run this script.
MSG
	exit 1
fi

dotnet run --project "$BACKEND_PROJECT"
