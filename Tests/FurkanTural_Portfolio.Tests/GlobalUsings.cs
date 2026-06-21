// Global using declarations to satisfy production source files compiled into the test project.
// Production files rely on Microsoft.NET.Sdk.Web implicit usings; the test project uses
// Microsoft.NET.Sdk, so we re-declare the missing namespaces here explicitly.

global using Microsoft.Extensions.Configuration;
global using Microsoft.Extensions.Logging;
global using System.Net.Http;
global using System.Net.Http.Json;
global using System.Text.Json;
global using System.ComponentModel.DataAnnotations;
