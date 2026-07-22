#!/bin/sh
set -e

mkdir -p /app/wallet
cp /etc/secrets/tnsnames.ora /app/wallet/tnsnames.ora
cp /etc/secrets/sqlnet.ora /app/wallet/sqlnet.ora
base64 -d /etc/secrets/cwallet.sso.b64 > /app/wallet/cwallet.sso

export TNS_ADMIN=/app/wallet
exec dotnet EventManagementSystem.dll
