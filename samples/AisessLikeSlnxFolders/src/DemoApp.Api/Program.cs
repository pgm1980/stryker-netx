// DemoApp.Api — Sprint 159 Fix-4 integration-test fixture.
// Minimal ASP.NET Core stub: "Hello World" endpoint.
// This project exists so the solution has a Web/API layer matching the
// Aisess 4-layer DDD-Onion structure. It is not invoked by the tests;
// only the Domain/Application/Infrastructure mutation targets matter.

using Microsoft.AspNetCore.Builder;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

app.MapGet("/", () => "Hello World");

app.Run();
