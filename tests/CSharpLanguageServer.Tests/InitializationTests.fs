module CSharpLanguageServer.Tests.InitializationTests

open NUnit.Framework

open CSharpLanguageServer.Tests.Tooling

[<TestCase>]
let testServerRegistersCapabilitiesWithTheClient () =
    let projectFiles =
        Map.ofList [
          ("Project/Project.csproj",
           """<Project Sdk="Microsoft.NET.Sdk">
                <PropertyGroup>
                  <OutputType>Exe</OutputType>
                  <TargetFramework>net8.0</TargetFramework>
                </PropertyGroup>
              </Project>
           """);
          ("Project/Class.cs",
           """using System;
              class Class
              {
              }
           """
          )
        ]

    use client = startAndMountServer projectFiles false
    client.Start()
    client.Initialize()
    client.WaitForProgressEnd("OK, 1 project file(s) loaded")
    Assert.IsTrue(client.ServerDidRespondTo("initialize"))
    Assert.IsTrue(client.ServerDidRespondTo("initialized"))
