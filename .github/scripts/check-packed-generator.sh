#!/usr/bin/env bash
# Checks the packed packages the way consumers get them, which the tests can't: they reference
# the source generator as a project analyzer. Builds, outside the repo, two projects that get
# Hlibz.MinimalApi.Endpoints only indirectly:
#   Shared  a class library referencing only Hlibz.MinimalApi.Endpoints.FluentValidation,
#   Api     a web app referencing only Shared,
# each with one endpoint, then runs Api: EndpointDiscovery must find both endpoints. Discovery
# throws for an assembly the generator didn't run on, so a generator that is missing from the
# package, doesn't load or doesn't reach an indirect consumer fails the check.
#
# Usage: check-packed-generator.sh <directory holding the .nupkg files>
set -euo pipefail

artifacts="$(cd "$1" && pwd)"
package="$(ls "$artifacts"/Hlibz.MinimalApi.Endpoints.[0-9]*.nupkg)"
version="$(basename "$package" .nupkg)"
version="${version#Hlibz.MinimalApi.Endpoints.}"

# The physical path: restore drops a project reference reached through a symlink (macOS /var).
work="$(cd "$(mktemp -d)" && pwd -P)"
trap 'rm -rf "$work"' EXIT
# Its own package cache: the packed version must never land in the machine-wide one.
export NUGET_PACKAGES="$work/packages"

# The packages only from the fresh pack; FluentValidation and the .NET 10 reference packs from nuget.org.
cat > "$work/nuget.config" <<EOF
<configuration>
  <packageSources>
    <clear />
    <add key="artifacts" value="$artifacts" />
    <add key="nuget.org" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
  <packageSourceMapping>
    <packageSource key="artifacts">
      <package pattern="Hlibz.MinimalApi.Endpoints" />
      <package pattern="Hlibz.MinimalApi.Endpoints.*" />
    </packageSource>
    <packageSource key="nuget.org">
      <package pattern="*" />
    </packageSource>
  </packageSourceMapping>
</configuration>
EOF

mkdir "$work/Shared" "$work/Api"

cat > "$work/Shared/Shared.csproj" <<EOF
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <FrameworkReference Include="Microsoft.AspNetCore.App" />
    <PackageReference Include="Hlibz.MinimalApi.Endpoints.FluentValidation" Version="$version" />
  </ItemGroup>
</Project>
EOF

cat > "$work/Shared/SharedEndpoints.cs" <<'EOF'
using Hlibz.MinimalApi.Endpoints;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;

namespace Shared;

public sealed class PingEndpointGroup : IEndpointGroup
{
    public string Prefix => "ping";
}

public sealed class SharedPingEndpoint : IEndpoint<PingEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) => group.MapGet("shared", () => "pong");
}
EOF

cat > "$work/Api/Api.csproj" <<'EOF'
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="../Shared/Shared.csproj" />
  </ItemGroup>
</Project>
EOF

cat > "$work/Api/Program.cs" <<'EOF'
using Hlibz.MinimalApi.Endpoints;
using Shared;

IReadOnlyList<EndpointGroupDescriptor> groups = EndpointDiscovery.Discover(
    [typeof(ApiPingEndpoint).Assembly, typeof(SharedPingEndpoint).Assembly]);
string[] endpoints = [.. groups.SelectMany(group => group.Endpoints).Select(endpoint => endpoint.EndpointType.Name)];
Console.WriteLine($"Endpoints found: {string.Join(", ", endpoints)}");
return endpoints.Length == 2 ? 0 : 1;

internal sealed class ApiPingEndpoint : IEndpoint<PingEndpointGroup>
{
    public RouteHandlerBuilder Map(IEndpointRouteBuilder group) => group.MapGet("api", () => "pong");
}
EOF

echo "Checking Hlibz.MinimalApi.Endpoints(.FluentValidation) $version from $artifacts"
dotnet run --project "$work/Api/Api.csproj"
