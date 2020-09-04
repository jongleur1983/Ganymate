﻿namespace Ganymate.Cli

open System
open System.IO

open Argu

open Ganymate

module Program =
    let printVersion () =
        let productName, version =
            let assembly = System.Reflection.Assembly.GetExecutingAssembly()

            let fileVersionInfo =
                System.Diagnostics.FileVersionInfo.GetVersionInfo assembly.Location

            fileVersionInfo.ProductName, assembly.GetName().Version

        printfn "%s v%i.%i.%i" productName version.Major version.Minor version.Build

    let traceColored color (s : string) =
        let curColor = Console.ForegroundColor
        if curColor <> color then Console.ForegroundColor <- color
        use textWriter =
            match color with
            | ConsoleColor.Red -> Console.Error
            | ConsoleColor.Yellow -> Console.Out
            | _ -> Console.Out

        textWriter.WriteLine s
        if curColor <> color then Console.ForegroundColor <- curColor

    type GanymateExiter() =
        interface IExiter with
            member __.Name = "Ganymate exiter"
            member __.Exit (msg, code) =
                if code = ErrorCode.HelpText then
                    printfn "%s" msg ; exit 0
                else traceColored ConsoleColor.Red msg ; exit 1

    [<CliPrefix(CliPrefix.DoubleDash)>]
    [<NoAppSettings>]
    type CliArgument =
        | [<MainCommand>] Directory of string
        with
            interface IArgParserTemplate with
                member arg.Usage =
                    match arg with
                    | Directory _ -> "The directory containing the Git repository"

    type ExitCode =
        | Ok = 0
        | NoDirectory = 1
        | NoRepository = 2

    let getRepositoryInfo directory =
        [
            sprintf "Showing commits in %s" directory
            sprintf "HEAD is %s" (GitRepository.getHead directory)
        ]
        
    [<EntryPoint>]
    let main argv =
        let parser =
            ArgumentParser.Create<CliArgument>(
                programName = "Ganymate",
                errorHandler = GanymateExiter())

        let results = parser.ParseCommandLine argv

        printfn "Got parse results %A" <| results.GetAllResults()
        let directory =
            results.GetResult(Directory, defaultValue = ".")
            |> function
                | "." -> Directory.GetCurrentDirectory()
                | directory -> directory

        let exitCode, output =
            if Directory.Exists directory
            then
                if GitRepository.isGitRepository directory
                then ExitCode.Ok, getRepositoryInfo directory
                else ExitCode.NoRepository, [ sprintf "Directory %s is not a Git repository" directory ]
            else ExitCode.NoDirectory, [ sprintf "%s is not a directory" directory ]

        output
        |> List.iter (printfn "%s")
        
        int exitCode
