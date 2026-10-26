#!/bin/bash
set -eu
export MSSQL_SA_PASSWORD="$(cat /handoff/sql-password)"
exec /opt/mssql/bin/sqlservr
