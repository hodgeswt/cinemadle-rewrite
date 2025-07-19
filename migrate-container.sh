#!/bin/sh

echo "Running EF Core migrations..."

dotnet ef database update --context DatabaseContext
dotnet ef database update --context IdentityContext
